﻿using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
        public bool IsRequired { get; private set; }
        /// <summary>
        /// Relevant attributes for this contract member.
        /// </summary>
        public IContractMemberAttribute[] Attributes { get; } = Array.Empty<IContractMemberAttribute>();
        /// <summary>
        /// Returns true if this property is transformed by a secure attribute.
        /// </summary>
        public bool HasSecureAttribute => Attributes.OfType<AbstractSecureValueAttribute>().Any();

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

        private static readonly ConcurrentDictionary<Type, ContractMemberDefinition[]> _memberCache = new ConcurrentDictionary<Type, ContractMemberDefinition[]>();

        /// <summary>
        /// Convert properties of any object to <see cref="ContractMemberDefinition"/>s.
        /// </summary>
        public static ContractMemberDefinition[] GetFromContract(object? obj, Type contractType)
        {
            // If runtime contract type, must have object
            if (contractType == typeof(IPhormContract))
            {
                if (obj == null)
                {
                    return Array.Empty<ContractMemberDefinition>();
                }
                contractType = obj.GetType();
            }

            var members = _memberCache.GetOrAdd(contractType,
                _ => getMemberDefs(obj, contractType));
            return members;
        }

        private static ContractMemberDefinition[] getMemberDefs(object? obj, Type contractType)
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

                // Resolve member direction
                var cmAttr = prop.GetCustomAttribute<ContractMemberAttribute>();
                var canRead = prop.CanRead && cmAttr?.DisableInput != true;
                var canWrite = prop.CanWrite && cmAttr?.DisableOutput != true;
                var dir = (canRead ? ParameterType.Input : 0) | (canWrite ? ParameterType.Output : 0);
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition()  == typeof(ContractOutMember<>))
                {
                    dir = ParameterType.Output;
                }

                // Ignore unusable properties
#if !NET5_0_OR_GREATER
                if (!dir.IsOneOf(ParameterType.Input, ParameterType.Output, ParameterType.InputOutput, ParameterType.ReturnValue))
#else
                if (dir is not ParameterType.Input and not ParameterType.Output and not ParameterType.InputOutput and not ParameterType.ReturnValue)
#endif
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
            }

            return members.ToArray();
        }

        /// <summary>
        /// Create an instance of this member by resolving the value from the appropriate entity member.
        /// </summary>
        public ContractMember FromEntity(object? entity)
        {
            object? getValue(MemberInfo? mem)
            {
                return mem switch
                {
                    PropertyInfo pi => pi.GetValue(entity),
                    MethodInfo mi => mi.Invoke(entity, Array.Empty<object>()),
                    _ => null
                };
            }

            object? value = null;
            if (entity != null && SourceMember != null)
            {
                var objType = entity.GetType();
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
        public ContractMember FromDatasource(object? value, object? entity)
        {
            var memb = this as ContractMember ?? new ContractMember(this, null);

            if (value == DBNull.Value)
            {
                value = null;
            }
            if (memb.Attributes.Length > 0)
            {
                // AbstractSecureValue
                var secvalAttr = memb.Attributes
                    .OfType<AbstractSecureValueAttribute>().SingleOrDefault();
                if (secvalAttr != null)
                {
                    value = secvalAttr.Decrypt((byte[]?)value, entity);
                }

                // Transformation
                var transfAttr = memb.Attributes
                    .OfType<AbstractTransphormAttribute>().SingleOrDefault();
                if (transfAttr != null)
                {
                    value = transfAttr.FromDatasource(ValueType, value, entity);
                }
            }

            memb.SetValue(value);
            return memb;
        }
    }
}
