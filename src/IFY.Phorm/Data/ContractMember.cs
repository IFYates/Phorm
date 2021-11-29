using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

// TODO: simplify
namespace IFY.Phorm.Data
{
    /// <summary>
    /// A property on a contract, with type helping.
    /// Supports in (to database) and out (from database) as well as the special-case return-value
    /// TODO: Column cover
    /// </summary>
    public abstract class ContractMember
    {
        /// <summary>
        /// Name as given in stored procedure
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Size of data to/from database
        /// 0 is unspecified / unlimited
        /// </summary>
        public int Size { get; } // TODO: not yet set
        /// <summary>
        /// Value being passed to or returned from stored procedure
        /// </summary>
        public object? Value { get; protected set; }
        /// <summary>
        /// Value has changed since originally set.
        /// </summary>
        public bool HasChanged { get; protected set; }
        /// <summary>
        /// Allowed direction of value from POV of sproc
        /// </summary>
        public ParameterDirection Direction { get; }
        /// <summary>
        /// Property of underlying DTO/Contract used to map types
        /// </summary>
        public PropertyInfo? SourceProperty { get; }
        /// <summary>
        /// The true type of the value, even if null
        /// Can be different to property value
        /// </summary>
        public Type ValueType { get; init; }
        /// <summary>
        /// Relevant attributes for this contract member.
        /// </summary>
        public IContractMemberAttribute[] Attributes { get; set; } = Array.Empty<IContractMemberAttribute>();

        protected ContractMember(string? name, object? value, ParameterDirection dir, PropertyInfo? sourceProperty)
        {
            Name = name ?? string.Empty;
            SourceProperty = sourceProperty;
            ValueType = sourceProperty?.PropertyType ?? typeof(object);
            SetValue(value);
            Direction = dir;
            HasChanged = false;
        }

        public static ContractMember<T> In<T>(string name, T value, PropertyInfo? sourceProperty = null)
        {
            return new ContractMember<T>(name, value, ParameterDirection.Input, sourceProperty);
        }
        public static ContractMember<T> InOut<T>(string name, T value, PropertyInfo? sourceProperty = null)
        {
            return new ContractMember<T>(name, value, ParameterDirection.InputOutput, sourceProperty);
        }
        public static ContractMember<T> Out<T>()
        {
            return new ContractMember<T>(string.Empty, default, ParameterDirection.Output);
        }
        public static ContractMember<T> Out<T>(string name, PropertyInfo? sourceProperty = null)
        {
            return new ContractMember<T>(name, default, ParameterDirection.Output, sourceProperty);
        }
        public static ContractMember<int> RetVal()
        {
            return new ContractMember<int>("return", 0, ParameterDirection.ReturnValue);
        }

        public void ResolveAttributes(object? context, out bool isSecure)
        {
            if (SourceProperty != null && Attributes.Length == 0)
            {
                Attributes = SourceProperty.GetCustomAttributes().OfType<IContractMemberAttribute>().ToArray();
            }
            Attributes.ToList().ForEach(a => a.SetContext(context));
            isSecure = Attributes.OfType<AbstractSecureValueAttribute>().Any();
        }

        public IDataParameter ToDataParameter(IAsyncDbCommand cmd)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = "@" + Name;
            param.Direction = Direction;
            if (Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            {
                param.Size = Size > 0 ? Size : 256;
            }

            // Transformation
            var transfAttr = Attributes.OfType<AbstractTransphormAttribute>().SingleOrDefault();
            var val = Value;
            if (transfAttr != null)
            {
                val = transfAttr.ToDatasource(val);
            }
            if (val != null)
            {
                if (val.GetType().IsEnum)
                {
                    val = (int)val;
                    param.DbType = DbType.Int32;
                }
                else if (val is DateTime dt)
                {
                    param.DbType = DbType.DateTime2;
                    // DateTime must be shifted in to SQL date range
                    if (dt <= SqlDateTime.MinValue.Value)
                    {
                        val = SqlDateTime.MinValue.Value;
                    }
                    else if (dt >= SqlDateTime.MaxValue.Value)
                    {
                        val = SqlDateTime.MaxValue.Value;
                    }
                }
            }

            // TODO: Is this needed as also in PhormContractRunner?
            // Check for DataMemberAttribute
            var dmAttr = SourceProperty?.GetCustomAttribute<DataMemberAttribute>();
            if (dmAttr != null)
            {
                // Primitives are never "missing", so only check null
                if (dmAttr.IsRequired && val == null)
                {
                    throw new ArgumentNullException(Name, $"Parameter {Name} for contract {SourceProperty?.ReflectedType?.FullName} is required but was null");
                }
            }

            // Apply value
            param.Value = val ?? DBNull.Value; // Must send non-null
            if (val == null)
            {
                // NOTE: Ignoring for Output as breaks fixed-char args - do not know full impact
                if (ValueType == typeof(string) && Direction != ParameterDirection.Output)
                {
                    // Fixes execution issue
                    param.Size = Size > 0 ? Size : 256;
                }
            }

            if (param.Value is Guid)
            {
                param.DbType = DbType.Guid;
            }

            if (Attributes.Length > 0)
            {
                // AbstractSecureValue
                var secvalAttr = Attributes.OfType<AbstractSecureValueAttribute>().SingleOrDefault();
                if (secvalAttr != null)
                {
                    param.Value = secvalAttr.Encrypt(param.Value);
                }
            }

            if (param.Value is byte[] bin)
            {
                param.DbType = DbType.Binary;
                param.Size = bin.Length;
            }
            else if (ValueType == typeof(byte[]))
            {
                param.DbType = DbType.Binary;
            }

            return param;
        }

        public void FromDatasource(object? val)
        {
            if (val == DBNull.Value)
            {
                val = null;
            }
            if (Attributes.Length > 0)
            {
                // AbstractSecureValue
                var secvalAttr = Attributes.OfType<AbstractSecureValueAttribute>().SingleOrDefault();
                if (secvalAttr != null)
                {
                    val = secvalAttr.Decrypt((byte[]?)val);
                }

                // Transformation
                var transfAttr = Attributes.OfType<AbstractTransphormAttribute>().SingleOrDefault();
                if (transfAttr != null)
                {
                    val = transfAttr.FromDatasource(ValueType, val);
                }
            }

            SetValue(val);
        }

        public abstract void SetValue(object? value);
    }

    public class ContractMember<T> : ContractMember
    {
        public new T? Value => (T?)base.Value;

        internal ContractMember(string name, T? value, ParameterDirection dir)
            : base(name, value, dir, null)
        {
            ValueType = typeof(T);
        }
        internal ContractMember(string name, T? value, ParameterDirection dir, PropertyInfo? sourceProperty)
            : base(name, value, dir, sourceProperty)
        {
            if (ValueType == typeof(object))
            {
                ValueType = typeof(T);
            }
        }

        public override void SetValue(object? value)
        {
            if (value != null)
            {
                var targetType = typeof(T) != typeof(object) ? typeof(T)
                    : ValueType != typeof(object) ? ValueType
                    : null;
                if (targetType != null && !targetType.IsInstanceOfType(value))
                {
                    targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                    value = Convert.ChangeType(value, targetType);
                }
            }
            if (base.Value != value)
            {
                base.Value = value;
                HasChanged = true;
            }
        }
    }
}
