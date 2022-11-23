using IFY.Phorm.Encryption;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using System.Data;
using System.Data.SqlTypes;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Data;

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

    public static ContractOutMember<T> InOut<T>(T value) => new(value);
    public static ContractOutMember<T> Out<T>() => new();
    public static ReturnValueMember RetVal() => new();
    public static ConsoleLogMember Console() => new();

    /// <summary>
    /// Convert properties of any object to <see cref="ContractMember"/>s.
    /// </summary>
    public static ContractMember[] GetMembersFromContract(object? obj, Type contractType, bool withReturnValue)
    {
        // If runtime contract type, must have object
        if (contractType == typeof(IPhormContract))
        {
            contractType = obj?.GetType() ?? typeof(IPhormContract);
        }
        var defs = ContractMemberDefinition.GetFromContract(contractType);

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

    // TODO: Extensible
    internal IDataParameter? ToDataParameter(IAsyncDbCommand cmd, object? context)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = "@" + DbName;
        param.Direction = (ParameterDirection)(int)Direction;
        if (Direction is ParameterType.Output or ParameterType.InputOutput)
        {
            param.Size = Size > 0 ? Size : 256;
        }

        if (!transformParameter(param, context))
        {
            return null;
        }

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

        param.Value ??= DBNull.Value; // Must send non-null
        return param;
    }

    private bool transformParameter(IDbDataParameter param, object? context)
    {
        // Transformation
        var transfAttr = Attributes.OfType<AbstractTransphormAttribute>().SingleOrDefault();
        var val = Value;
        if (transfAttr != null)
        {
            val = transfAttr.ToDatasource(val, context);
            if (val is IgnoreDataMemberAttribute || (val as Type) == typeof(IgnoreDataMemberAttribute))
            {
                return false;
            }
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

                // Must be shifted in to SQL date range
                if (dt < SqlDateTime.MinValue.Value)
                {
                    val = SqlDateTime.MinValue.Value;
                }
                else if (dt > SqlDateTime.MaxValue.Value)
                {
                    val = SqlDateTime.MaxValue.Value;
                }
            }
#if NET6_0_OR_GREATER
            else if (val is DateOnly date)
            {
                param.DbType = DbType.Date;

                // Must be shifted in to SQL date range
                dt = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                if (dt < SqlDateTime.MinValue.Value)
                {
                    val = DateOnly.FromDateTime(SqlDateTime.MinValue.Value);
                }
                // Currently impossible, given that DateOnly.MaxValue < SqlDateTime.MaxValue
                //else if (dt > SqlDateTime.MaxValue.Value)
                //{
                //    val = DateOnly.FromDateTime(SqlDateTime.MaxValue.Value);
                //}
            }
#endif
        }
        param.Value = val;

        // Check if required (Primitives are never "missing", so only check null)
        if (IsRequired && param.Value == null)
        {
            throw new ArgumentNullException(DbName, $"Parameter {DbName} for contract {SourceMember?.DeclaringType?.FullName} is required but was null");
        }

        return true;
    }

    /// <summary>
    /// Apply this value to an entity.
    /// </summary>
    public void ApplyToEntity(object entity)
    {
        var prop = SourceMember as PropertyInfo;
        try
        {
            if (prop?.DeclaringType?.IsAssignableFrom(entity.GetType()) == false)
            {
                prop = entity.GetType().GetProperty(prop.Name);
            }
            if (prop?.PropertyType.IsGenericType == true
                && typeof(ContractOutMember<>) == prop.PropertyType.GetGenericTypeDefinition())
            {
                var val = (ContractMember)prop.GetValue(entity);
                val.SetValue(Value);
            }
            else if (prop?.SetMethod != null)
            {
                prop.SetValue(entity, Value);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set property {SourceMember?.DeclaringType?.FullName ?? "(unknown)"}.{SourceMember?.Name ?? DbName}", ex);
        }
    }

    /// <summary>
    /// Set this value as from a datasource.
    /// </summary>
    /// <param name="value">The value from the datasource.</param>
    /// <param name="source">The DTO that is being built from the datasource.</param>
    public bool SetFromDatasource(object? value, object? source)
    {
        if (value == DBNull.Value)
        {
            value = null;
        }

        if (Attributes.Any())
        {
            // AbstractSecureValue
            var secvalAttr = Attributes
                .OfType<AbstractSecureValueAttribute>().SingleOrDefault();
            if (secvalAttr != null)
            {
                value = secvalAttr.Decrypt((byte[]?)value, source);
            }

            // Transformation
            var transfAttr = Attributes
                .OfType<AbstractTransphormAttribute>().SingleOrDefault();
            if (transfAttr != null)
            {
                value = transfAttr.FromDatasource(ValueType, value, source);
                if (value is IgnoreDataMemberAttribute || (value as Type) == typeof(IgnoreDataMemberAttribute))
                {
                    return false;
                }
            }
        }

        SetValue(value);
        return true;
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
                    value = value.ChangeType(targetType);
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

    internal ContractOutMember()
        : base(null, default, ParameterType.Output, typeof(T))
    { }
    internal ContractOutMember(T value)
        : base(null, value, ParameterType.InputOutput, typeof(T))
    { }
}

public sealed class ReturnValueMember : ContractMember
{
    public new int Value => (int)base.Value!;

    internal ReturnValueMember()
        : base("return", 0, ParameterType.ReturnValue, typeof(int))
    {
    }
}

public sealed class ConsoleLogMember : ContractMember
{
    public new ConsoleMessage[] Value => (ConsoleMessage[])base.Value!;

    internal ConsoleLogMember()
        : base("console", Array.Empty<ConsoleMessage>(), ParameterType.Console, typeof(ConsoleMessage[]))
    {
    }
}
