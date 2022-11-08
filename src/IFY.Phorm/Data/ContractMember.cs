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
    /// A property on a contract, with type helping.
    /// Supports in (to database) and out (from database) as well as the special-case return-value
    /// </summary>
    public class ContractMemberDefinition
    {
        /// <summary>
        /// Name as given in stored procedure.
        /// </summary>
        public string DbName { get; private set; }
        /// <summary>
        /// Size of data to/from database.
        /// 0 is unspecified / unlimited.
        /// </summary>
        public int Size { get; } // TODO: Not yet used. Drop if not needed.
        /// <summary>
        /// Type of parameter from POV of datasource.
        /// </summary>
        public ParameterType Direction { get; }
        /// <summary>
        /// Member of underlying DTO/Contract that provides the value.
        /// </summary>
        public MemberInfo? SourceMember { get; }
        /// <summary>
        /// Identifier for the contract member.
        /// </summary>
        public string? SourceMemberId { get; }
        /// <summary>
        /// The true type of the value, even if null.
        /// Can be different to property value.
        /// </summary>
        public Type ValueType { get; }
        /// <summary>
        /// Whether this member is marked as required on the contract.
        /// </summary>
        public bool IsRequired { get; private set; }
        /// <summary>
        /// Relevant attributes for this contract member.
        /// </summary>
        public IContractMemberAttribute[] Attributes { get; set; } = Array.Empty<IContractMemberAttribute>();

        internal ContractMemberDefinition(string? dbName, ParameterType dir, MethodInfo? sourceMethod, Type? valueType = null)
        {
            DbName = dbName ?? string.Empty;
            SourceMember = sourceMethod;
            SourceMemberId = sourceMethod != null ? $"{sourceMethod.Name}@{sourceMethod.DeclaringType!.FullName}" : null;
            ValueType = sourceMethod?.ReturnType ?? valueType ?? typeof(object);
            Direction = dir;
        }
        internal ContractMemberDefinition(string? dbName, ParameterType dir, PropertyInfo? sourceProperty, Type? valueType = null)
        {
            DbName = dbName ?? string.Empty;
            SourceMember = sourceProperty;
            SourceMemberId = sourceProperty != null ? $"{sourceProperty.Name}@{sourceProperty.DeclaringType!.FullName}" : null;
            ValueType = sourceProperty?.PropertyType ?? valueType ?? typeof(object);
            Direction = dir;
        }

        // TODO: Cache members by type?
        /// <summary>
        /// Convert properties of any object to <see cref="ContractMemberDefinition"/>s.
        /// </summary>
        public static ContractMemberDefinition[] ResolveContract(object? obj, Type contractType)
        {
            var hasContract = contractType != typeof(IPhormContract);
            if (!hasContract)
            {
                if (obj == null)
                {
                    return Array.Empty<ContractMemberDefinition>();
                }
                contractType = obj.GetType();
            }

            // Map all member properties
            var objType = obj?.GetType();
            var isContract = hasContract && (obj == null || contractType.IsAssignableFrom(objType));
            var props = contractType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var members = new List<ContractMemberDefinition>(props.Length);
            foreach (var prop in props)
            {
                PropertyInfo objProp = prop;
                if (!isContract)
                {
                    // Support non-contract
                    objProp = objType?.GetProperty(prop.Name, BindingFlags.Instance | BindingFlags.Public) ?? prop;
                }

                var cmAttr = prop.GetCustomAttribute<ContractMemberAttribute>();
                var canRead = prop.CanRead && cmAttr?.DisableInput != true;
                var canWrite = prop.CanWrite && cmAttr?.DisableOutput != true;
                var dir = (canRead ? ParameterType.Input : 0) | (canWrite ? ParameterType.Output : 0);

                if (obj != null && typeof(ContractMember).IsAssignableFrom(objProp.PropertyType))
                {
                    // Can use ContractMember to change behaviour
                    var cmValue = (ContractMember?)objProp.GetValue(obj);
                    dir = cmValue?.Direction ?? dir;
                }

                if (dir == 0
                    || prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                {
                    continue;
                }

                // Skip console members
                if (prop.PropertyType == typeof(ConsoleLogMember))
                {
                    continue;
                }

                // Check for DataMemberAttribute changes
                var dmAttr = prop.GetCustomAttribute<DataMemberAttribute>();

                var dbName = dmAttr?.Name ?? prop.Name;
                var memb = new ContractMemberDefinition(dbName, dir, prop)
                {
                    IsRequired = dmAttr?.IsRequired == true
                };

                members.Add(memb);
                memb.ResolveAttributes(obj, out _);

                // TODO: omit unused?
            }

            // Map additional member methods
            var methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !m.IsSpecialName // Ignore property methods
                    && m.CustomAttributes.Any(a => a.AttributeType == typeof(ContractMemberAttribute))) // Must have attribute
                .ToArray();
            foreach (var method in methods)
            {
                // Must not have any parameters
                if (method.GetParameters().Any())
                {
                    throw new InvalidDataContractException($"Cannot include method '{contractType.FullName}.{method.Name}' in contract: specifies parameters.");
                }

                var memb = new ContractMemberDefinition(method.Name, ParameterType.Input, method);

                members.Add(memb);
                memb.ResolveAttributes(obj, out _);
            }

            return members.ToArray();
        }

        public void ResolveAttributes(object? context, out bool isSecure)
        {
            if (SourceMember != null && Attributes.Length == 0)
            {
                Attributes = SourceMember.GetCustomAttributes().OfType<IContractMemberAttribute>().ToArray();
            }
            Attributes.ToList().ForEach(a => a.SetContext(context));
            isSecure = Attributes.OfType<AbstractSecureValueAttribute>().Any();
        }

        public ContractMember Fill(object? obj)
        {
            object? getValue(MemberInfo? mem)
            {
                if (mem is PropertyInfo pi)
                {
                    return pi.GetValue(obj);
                }
                else if (mem is MethodInfo mi)
                {
                    return mi.Invoke(obj, Array.Empty<object>());
                }
                return null;
            }

            object? value = null;
            if (obj != null && SourceMember != null)
            {
                var objType = obj.GetType();
                if (SourceMember.DeclaringType == objType)
                {
                    value = getValue(SourceMember);
                }
                else
                {
                    // Support non-contract
                    var anonProp = objType?.GetProperty(SourceMember.Name, BindingFlags.Instance | BindingFlags.Public);
                    value = getValue(anonProp);
                }
            }

            // Wrap as ContractMember, if not already
#if !NET5_0_OR_GREATER
            if (!(value is ContractMember memb))
#else
            if (value is not ContractMember memb)
#endif
            {
                // Can only be method or property
                memb = SourceMember is MethodInfo mi
                    ? new ContractMember<object?>(DbName, value, Direction, mi)
                    : new ContractMember<object?>(DbName, value, Direction, (PropertyInfo?)SourceMember);
            }
            else if (memb.Direction == ParameterType.ReturnValue)
            {
                return memb;
            }
            else
            {
                memb.DbName = DbName;
            }

            memb.Attributes = Attributes;
            // TODO: other properties?
            return memb;
        }
    }

    /// <summary>
    /// The current instance value of a contract member.
    /// </summary>
    public abstract class ContractMember : ContractMemberDefinition
    {
        /// <summary>
        /// Value being passed to or returned from stored procedure.
        /// </summary>
        public object? Value { get; protected set; }
        /// <summary>
        /// Value has changed since originally set.
        /// </summary>
        public bool HasChanged { get; protected set; }

        internal ContractMember(string? dbName, object? value, ParameterType dir, MethodInfo? sourceMethod, Type valueType)
            : base(dbName, dir, sourceMethod, valueType)
        {
            SetValue(value);
            HasChanged = false;
        }
        internal ContractMember(string? dbName, object? value, ParameterType dir, PropertyInfo? sourceProperty, Type valueType)
            : base(dbName, dir, sourceProperty, valueType)
        {
            SetValue(value);
            HasChanged = false;
        }

        internal static ContractMember<T> In<T>(string dbName, T value, PropertyInfo? sourceProperty = null)
        {
            return new ContractMember<T>(dbName, value, ParameterType.Input, sourceProperty);
        }
        public static ContractMember<T> Out<T>()
        {
            return new ContractMember<T>(string.Empty, default!, ParameterType.Output);
        }
        internal static ContractMember<T> Out<T>(string dbName, PropertyInfo? sourceProperty = null)
        {
            return new ContractMember<T>(dbName, default!, ParameterType.Output, sourceProperty);
        }
        public static ContractMember<int> RetVal()
        {
            return new ContractMember<int>("return", 0, ParameterType.ReturnValue);
        }
        public static ConsoleLogMember Console()
        {
            return new ConsoleLogMember();
        }

        /// <summary>
        /// Convert properties of any object to <see cref="ContractMember"/>s.
        /// </summary>
        public static ContractMember[] GetMembersFromContract(object? obj, Type contractType, bool withReturnValue)
        {
            var hasContract = contractType != typeof(IPhormContract);
            var objType = obj?.GetType();
            var isContract = hasContract && (obj == null || contractType.IsAssignableFrom(objType));

            var defs = ContractMemberDefinition.ResolveContract(obj, contractType);

            // Resolve member values
            var members = new List<ContractMember>(defs.Length);
            foreach (var def in defs)
            {
                var memb = def.Fill(obj);
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
                        .Where(p => p.PropertyType == typeof(ContractMember<int>))
                        .Select(p => p.GetValue(obj) as ContractMember<int>)
                        .FirstOrDefault(v => v?.Direction == ParameterType.ReturnValue);

                    members.Add(retPar ?? RetVal());
                }
                return members;
            }
        }

        public IDataParameter ToDataParameter(IAsyncDbCommand cmd)
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

            // Check for DataMemberAttribute
            var dmAttr = SourceMember?.GetCustomAttribute<DataMemberAttribute>();
            if (dmAttr != null)
            {
                // Primitives are never "missing", so only check null
                if (dmAttr.IsRequired && val == null)
                {
                    throw new ArgumentNullException(DbName, $"Parameter {DbName} for contract {SourceMember?.ReflectedType?.FullName} is required but was null");
                }
            }

            // Apply value
            param.Value = val ?? DBNull.Value; // Must send non-null
            if (val == null)
            {
                // NOTE: Ignoring for Output as breaks fixed-char args - do not know full impact
                if (ValueType == typeof(string) && Direction != ParameterType.Output)
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
        public new T Value => (T)base.Value!;

        internal ContractMember(string name, T value, ParameterType dir)
            : base(name, value, dir, (PropertyInfo?)null, typeof(T))
        { }
        internal ContractMember(string name, T value, ParameterType dir, MethodInfo? sourceMethod)
            : base(name, value, dir, sourceMethod, typeof(T))
        { }
        internal ContractMember(string name, T value, ParameterType dir, PropertyInfo? sourceProperty)
            : base(name, value, dir, sourceProperty, typeof(T))
        { }

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
            if (base.Value != value)
            {
                base.Value = value;
                HasChanged = true;
            }
        }
    }

    public sealed class ConsoleLogMember : ContractMember<ConsoleMessage[]>
    {
        public ConsoleLogMember()
            : base("console", Array.Empty<ConsoleMessage>(), ParameterType.Console)
        {
        }
    }
}
