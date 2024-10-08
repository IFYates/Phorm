﻿using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Data;

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
    public int Size { get; private set; } // TODO: Not yet used. Drop if not needed.
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
    public bool IsRequired { get; internal set; }
    /// <summary>
    /// Relevant attributes for this contract member.
    /// </summary>
    public IContractMemberAttribute[] Attributes { get; } = [];
    /// <summary>
    /// Returns true if this property is transformed by a secure attribute.
    /// </summary>
    public bool HasSecureAttribute => Attributes.OfType<AbstractSecureValueAttribute>().Any();
    /// <summary>
    /// Returns true if this property is transformed by an attribute.
    /// </summary>
    public bool HasTransphormation => Attributes.OfType<AbstractTransphormAttribute>().Any();

    internal ContractMemberDefinition(ContractMemberDefinition orig)
    {
        DbName = orig.DbName;
        Size = orig.Size;
        Direction = orig.Direction;
        SourceMember = orig.SourceMember;
        SourceMemberId = orig.SourceMemberId;
        ValueType = orig.ValueType;
        IsRequired = orig.IsRequired;
        Attributes = orig.Attributes;
    }
    internal ContractMemberDefinition(string? dbName, ParameterType dir, MethodInfo sourceMethod)
    {
        DbName = dbName ?? string.Empty;
        SourceMember = sourceMethod;
        SourceMemberId = $"{sourceMethod.Name}@{sourceMethod.DeclaringType!.FullName}";
        ValueType = sourceMethod.ReturnType;
        Direction = dir;
        Attributes = SourceMember.GetCustomAttributes().OfType<IContractMemberAttribute>().ToArray();
    }
    internal ContractMemberDefinition(string? dbName, ParameterType dir, PropertyInfo sourceProperty)
    {
        DbName = dbName ?? string.Empty;
        SourceMember = sourceProperty;
        SourceMemberId = $"{sourceProperty.Name}@{sourceProperty.DeclaringType!.FullName}";
        ValueType = sourceProperty.PropertyType;
        Direction = dir;
        Attributes = SourceMember.GetCustomAttributes().OfType<IContractMemberAttribute>().ToArray();
    }
    internal ContractMemberDefinition(string? dbName, ParameterType dir, Type valueType)
    {
        DbName = dbName ?? string.Empty;
        ValueType = valueType;
        Direction = dir;
    }

    private static readonly ConcurrentDictionary<Type, ContractMemberDefinition[]> _memberCache = new();

    /// <summary>
    /// Convert properties of any object to <see cref="ContractMemberDefinition"/>s.
    /// </summary>
    internal static ContractMemberDefinition[] GetFromContract(Type contractType)
    {
        var members = _memberCache.GetOrAdd(contractType,
            _ => getMemberDefs(contractType));
        return members;
    }

    private static ContractMemberDefinition[] getMemberDefs(Type contractType)
    {
        // Map all contract properties
        var contractProps = contractType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var members = new List<ContractMemberDefinition>(contractProps.Length);
        foreach (var prop in contractProps)
        {
            // Skip console members
            if (prop.PropertyType == typeof(ConsoleLogMember)
                || prop.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
            {
                continue;
            }

            var memb = fromPropertyInfo(prop);
            if (memb != null)
            {
                members.Add(memb);
            }
        }

        // Map additional member methods
        var methods = contractType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => !m.IsSpecialName // Ignore property methods
                && m.CustomAttributes.Any(a => a.AttributeType == typeof(ContractMemberAttribute))) // Must have attribute
            .ToArray();
        foreach (var method in methods)
        {
            // Must not have any parameters
            if (method.GetParameters().Length != 0)
            {
                throw new InvalidDataContractException($"Cannot include method '{contractType.FullName}.{method.Name}' in contract: specifies parameters.");
            }
            // Must have return type
            if (method.ReturnType == typeof(void))
            {
                throw new InvalidDataContractException($"Cannot include method '{contractType.FullName}.{method.Name}' in contract: void return type.");
            }

            var memb = new ContractMemberDefinition(method.Name, ParameterType.Input, method);

            members.Add(memb);
        }

        return members.ToArray();
    }

    private static ContractMemberDefinition? fromPropertyInfo(PropertyInfo prop)
    {
        // Resolve member direction
        var cmAttr = prop.GetCustomAttribute<ContractMemberAttribute>();
        var canRead = prop.CanRead && cmAttr?.DisableInput != true;
        var canWrite = prop.CanWrite && cmAttr?.DisableOutput != true;
        var dir = (canRead ? ParameterType.Input : 0) | (canWrite ? ParameterType.Output : 0);
        if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(ContractOutMember<>))
        {
            dir = ParameterType.Output;
        }

        // Ignore unusable properties
        if (dir is not ParameterType.Input and not ParameterType.Output and not ParameterType.InputOutput and not ParameterType.ReturnValue)
        {
            return null;
        }

        // Check for DataMemberAttribute changes
        var dmAttr = prop.GetCustomAttribute<DataMemberAttribute>();

        var dbName = dmAttr?.Name ?? prop.Name;
        return new ContractMemberDefinition(dbName, dir, prop)
        {
            IsRequired = dmAttr?.IsRequired == true
        };
    }

    /// <summary>
    /// Create an instance of this member by resolving the value from the appropriate entity member.
    /// </summary>
    internal ContractMember FromEntity(object? entity)
    {
        object? value = null;
        if (entity != null)
        {
            var objType = entity.GetType();
            if (SourceMember is PropertyInfo pi && pi.CanRead)
            {
                if (!pi.DeclaringType!.IsAssignableFrom(objType))
                {
                    // Resolve same property on non-contract
                    pi = objType.GetProperty(SourceMember.Name, BindingFlags.Instance | BindingFlags.Public)!;
                }
                value = pi?.GetValue(entity);
            }
            else if (SourceMember is MethodInfo mi)
            {
                if (!mi.DeclaringType!.IsAssignableFrom(objType))
                {
                    // Resolve identical method on non-contract
                    mi = objType.GetMethod(SourceMember.Name, BindingFlags.Instance | BindingFlags.Public)!;

                    // Otherwise, resolve property with same name on non-contract
                    mi ??= objType.GetProperty(SourceMember.Name, BindingFlags.Instance | BindingFlags.Public)?.GetMethod!;
                }
                value = mi?.Invoke(entity, []);
            }
        }

        // Wrap as ContractMember, if not already
        if (value is not ContractMember memb)
        {
            // Can only be method or property
            memb = new ContractMember(this, value);
        }
        else if (memb.Direction == ParameterType.ReturnValue)
        {
            return memb;
        }
        else
        {
            memb.DbName = DbName;
            memb.IsRequired = IsRequired;
            memb.Size = Size;
        }

        return memb;
    }

    /// <summary>
    /// Create an instance of this member by using the datasource value provided.
    /// </summary>
    internal bool TryFromDatasource(object? value, object? entity, [NotNullWhen(true)] out ContractMember? member)
    {
        var memb = this as ContractMember ?? new ContractMember(this, null);
        if (!memb.SetFromDatasource(value, entity))
        {
            member = null;
            return false;
        }

        member = memb;
        return true;
    }
}
