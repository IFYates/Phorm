using IFY.Phorm.Encryption;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Data
{
    /// <summary>
    /// The current instance value of a contract member.
    /// </summary>
    public class ContractMember : ContractMemberDefinition
    {
        /// <summary>
        /// Value being passed to or returned from stored procedure.
        /// </summary>
        public object? Value { get; private set; }
        /// <summary>
        /// Value has changed since originally set.
        /// </summary>
        public bool HasChanged { get; private set; }

        internal ContractMember(ContractMemberDefinition def, object? value)
            : base(def)
        {
            SetValue(value);
            HasChanged = false;
        }
        internal ContractMember(string? dbName, object? value, ParameterType dir, PropertyInfo sourceProperty)
            : base(dbName, dir, sourceProperty)
        {
            SetValue(value);
            HasChanged = false;
        }
        internal ContractMember(string? dbName, object? value, ParameterType dir, Type valueType)
            : base(dbName, dir, valueType)
        {
            SetValue(value);
            HasChanged = false;
        }

        public static ContractOutMember<T> InOut<T>(T value)
            => new ContractOutMember<T>(value);
        public static ContractOutMember<T> Out<T>()
            => new ContractOutMember<T>();
        public static ReturnValueMember RetVal()
            => new ReturnValueMember();
        public static ConsoleLogMember Console()
            => new ConsoleLogMember();

        /// <summary>
        /// Convert properties of any object to <see cref="ContractMember"/>s.
        /// </summary>
        public static ContractMember[] GetMembersFromContract(object? obj, Type contractType, bool withReturnValue)
        {
            var defs = ContractMemberDefinition.GetFromContract(contractType, obj?.GetType());

            // Resolve member values
            var members = new List<ContractMember>(defs.Length);
            foreach (var def in defs)
            {
                var memb = def.FromEntity(obj);
                members.Add(memb);

                // Primitives are never "missing", so only check null
                if (def.IsRequired && memb.Value == null)
                {
                    throw new ArgumentNullException(memb.DbName, $"Parameter {memb.DbName} for contract {contractType.FullName} is required but was null");
                }
            }

            // TODO: omit unused?

            if (!withReturnValue)
            {
                return members.ToArray();
            }
            return addReturnValue(members).ToArray();

            IList<ContractMember> addReturnValue(IList<ContractMember> members)
            {
                if (!members.Any(p => p.Direction == ParameterType.ReturnValue))
                {
                    // Allow for a return value on the object
                    var retPar = obj?.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.PropertyType == typeof(ReturnValueMember))
                        .Select(p => p.GetValue(obj) as ReturnValueMember)
                        .FirstOrDefault(v => v?.Direction == ParameterType.ReturnValue);

                    members.Add(retPar ?? RetVal());
                }
                return members;
            }
        }

        public IDataParameter ToDataParameter(IAsyncDbCommand cmd, object? context)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = "@" + DbName;
            param.Direction = (ParameterDirection)(int)Direction;
#if !NET5_0_OR_GREATER
            if (Direction.IsOneOf(ParameterType.Output, ParameterType.InputOutput))
#else
            if (Direction is ParameterType.Output or ParameterType.InputOutput)
#endif
            {
                param.Size = Size > 0 ? Size : 256;
            }

            transformParameter(param, context);

            // Apply value
            if (param.Value == null)
            {
                // NOTE: Ignoring for Output as breaks fixed-char args - do not know full impact
                if (ValueType == typeof(string) && Direction != ParameterType.Output)
                {
                    // Fixes execution issue
                    param.Size = Size > 0 ? Size : 256;
                }
            }

            param.Value ??= DBNull.Value; // Must send non-null

            if (param.Value is Guid)
            {
                param.DbType = DbType.Guid;
            }

            if (HasSecureAttribute)
            {
                // AbstractSecureValue
                var secvalAttr = Attributes.OfType<AbstractSecureValueAttribute>().Single();
                param.Value = secvalAttr.Encrypt(param.Value, context);
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

        private void transformParameter(IDbDataParameter param, object? context)
        {
            // Transformation
            var transfAttr = Attributes.OfType<AbstractTransphormAttribute>().SingleOrDefault();
            var val = Value;
            if (transfAttr != null)
            {
                val = transfAttr.ToDatasource(val, context);
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
            param.Value = val;

            // Check for DataMemberAttribute
            var dmAttr = SourceMember?.GetCustomAttribute<DataMemberAttribute>();
            if (dmAttr != null)
            {
                // Primitives are never "missing", so only check null
                if (dmAttr.IsRequired && param.Value == null)
                {
                    throw new ArgumentNullException(DbName, $"Parameter {DbName} for contract {SourceMember?.ReflectedType?.FullName} is required but was null");
                }
            }
        }

        /// <summary>
        /// Apply this value to an entity.
        /// </summary>
        public void ApplyToEntity(object entity)
        {
            try
            {
                ((PropertyInfo?)SourceMember)?.SetValue(entity, Value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set property {SourceMember?.Name ?? DbName}", ex);
            }
        }

        internal void SetValue(object? value)
        {
            if (value != null)
            {
                var targetType = ValueType != typeof(object)
                    ? ValueType
                    : null;
                if (targetType != null && !targetType.IsInstanceOfType(value))
                {
                    targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                    if (value is byte[] bytes)
                    {
                        value = bytes.FromBytes(targetType);
                    }
                    else
                    {
                        value = Convert.ChangeType(value, targetType);
                    }
                }
            }
            Value = value;
            HasChanged = true;
        }
    }

    public sealed class ContractOutMember<T> : ContractMember
    {
        public new T Value => (T)base.Value!;

        public ContractOutMember()
            : base(null, default, ParameterType.Output, typeof(T))
        { }
        public ContractOutMember(T value)
            : base(null, value, ParameterType.InputOutput, typeof(T))
        { }
    }

    public sealed class ReturnValueMember : ContractMember
    {
        public new int Value => (int)base.Value!;

        public ReturnValueMember()
            : base("return", 0, ParameterType.ReturnValue, typeof(int))
        {
        }
    }

    public sealed class ConsoleLogMember : ContractMember
    {
        public new ConsoleMessage[] Value => (ConsoleMessage[])base.Value!;

        public ConsoleLogMember()
            : base("console", Array.Empty<ConsoleMessage>(), ParameterType.Console, typeof(ConsoleMessage[]))
        {
        }
    }
}
