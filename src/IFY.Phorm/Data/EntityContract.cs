using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using System.Reflection;

namespace IFY.Phorm.Data;

// TODO
// var ec = EntityContract.Prepare(entityType) // Cached
// > Prebuild resolver for a type and cache for reuse
// > Determine if record with primary constructor, grab all properties with meta
// foreach (rowData) Func<object> ir = ec.GetInstanceResolver(rowData, rowIndex, cmdGuid)
// > Warn on unknown/unused members (inc. record primary constructor)
// inst = ir.CreateInstance() // For deferred resolution
// > If record primary constructor, executes first; else, creates default instance
// > Applies basic property values (for record, set what can and use "with" batch for rest)
// > Apply deferred properties (same record logic)
internal class EntityContract
{
#pragma warning disable CS8618 // No 'required' or 'init' in .NET Standard 2.1
    private EntityContract(Type entityType)
    {
        EntityType = entityType;
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the type of the entity this contract represents.
    /// </summary>
    public Type EntityType { get; private set; }
    /// <summary>
    /// Gets the list of members defined in the contract.
    /// </summary>
    public IReadOnlyList<ContractMemberDefinition> Members { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current instance represents a record.
    /// </summary>
    public bool IsRecord { get; private set; }
    /// <summary>
    /// Gets the constructor information for the primary constructor of the record, if applicable.
    /// </summary>
    public ConstructorInfo Constructor { get; private set; }
    /// <summary>
    /// Gets the array of argument names used to construct the instance, or null if no arguments were provided.
    /// </summary>
    public ContractMemberDefinition[] ConstructorFields { get; private set; }

    /// <summary>
    /// Gets the property setters for basic properties that can be set directly.
    /// </summary>
    public ContractMemberDefinition[] BasicPropertySetters { get; private set; }
    /// <summary>
    /// Gets the property setters for deferred properties that require special handling.
    /// </summary>
    public ContractMemberDefinition[] DeferredPropertySetters { get; private set; }

    // Find all entity members and how they will be applied
    // Secure/Transformed properties are later (warn on use in primary constructor)
    // Support members on record primary constructor
    // Support setting record backing fields for unsettable properties
    public static EntityContract Prepare(Type entityType)
    {
        var isRecord = entityType.GetMethod("<Clone>$") != null;
        var members = ContractMemberDefinition.GetFromContract(entityType)
            .Where(static m => m.SourceMember is PropertyInfo) // Should never be others
            .Where(static m => m.Direction is ParameterType.Output or ParameterType.InputOutput)
            .ToArray();
        var memberDict = members.ToDictionary(static m => m.SourceMember!.Name);

        // Determine record primary constructor and its parameters
        ConstructorInfo? ctor = null;
        ContractMemberDefinition[] constructorFields = [];
        if (isRecord)
        {
            (ctor, constructorFields) = findPrimaryConstructor(entityType, memberDict);
            if (ctor != null)
            {
                // Remove constructor members from available members
                foreach (var field in constructorFields)
                {
                    // TODO: Warn if member is secure/transformed (not supported on constructor)
                    memberDict.Remove(field.SourceMember!.Name);
                }
            }
        }

        return new EntityContract(entityType)
        {
            IsRecord = isRecord,
            Members = members,

            Constructor = ctor ?? entityType.GetConstructor(Type.EmptyTypes)!,
            ConstructorFields = constructorFields,

            // Map members to basic and deferred property setters
            BasicPropertySetters = memberDict.Values
                .Where(static m => !m.HasSecureAttribute && !m.HasTransphormation)
                .ToArray(),
            DeferredPropertySetters = memberDict.Values
                .Where(static m => m.HasSecureAttribute || m.HasTransphormation)
                .ToArray()
        };
    }

    /// <summary>
    /// Finds the primary constructor for a record type.
    /// The primary constructor is defined as the one with the most parameters that are exposed as properties.
    /// </summary>
    private static (ConstructorInfo? constructor, ContractMemberDefinition[] constructorFields) findPrimaryConstructor(Type type, Dictionary<string, ContractMemberDefinition> members)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        ContractMemberDefinition[] constructorFields = [];
        ConstructorInfo? primaryConstructor = null;
        foreach (var constructor in constructors)
        {
            // Check if all constructor parameters match available members
            var argMembers = constructor.GetParameters()
                .Select(arg => members.TryGetValue(arg.Name ?? "", out var member) ? member : null)
                .ToArray();
            if (!argMembers.Any(static m => m is null))
            {
                // Remember the constructor with the most property parameters
                if (argMembers.Length > constructorFields.Length)
                {
                    primaryConstructor = constructor;
                    constructorFields = argMembers!;
                }
            }
        }
        return (primaryConstructor, constructorFields);
    }

    // TODO: when would this return null?
    public Func<object>? GetInstanceResolver(Dictionary<string, object?> rowData)
    {
        return () => FillInstance(
            FillInstance(
                BuildInstance(rowData),
                rowData, BasicPropertySetters),
            rowData, DeferredPropertySetters);
    }

    public bool IsResultsetValid(Dictionary<string, object?> rowData, AbstractPhormSession session, Guid commandGuid)
    {
        // Check for additional columns in rowData
        foreach (var fieldName in rowData.Keys)
        {
            if (!Members.Any(m => m.DbName == fieldName))
            {
                session.OnUnexpectedRecordColumn(new UnexpectedRecordColumnEventArgs
                {
                    CommandGuid = commandGuid,
                    EntityType = EntityType,
                    ColumnName = fieldName
                });
            }
        }

        // Check for unspecified contract members
        var unhandledFields = Members.Where(m => !rowData.ContainsKey(m.DbName))
            .Select(static m => m.DbName).ToArray();
        if (unhandledFields.Length > 0)
        {
            session.OnUnresolvedContractMember(new UnresolvedContractMemberEventArgs
            {
                CommandGuid = commandGuid,
                EntityType = EntityType,
                MemberNames = unhandledFields
            });
        }

        return true;
    }

    public object BuildInstance(Dictionary<string, object?> rowData)
    {
        // Prepare constructor arguments
        var args = new object?[ConstructorFields.Length];
        for (int i = 0; i < args.Length; i++)
        {
            var member = new ContractMember(ConstructorFields[i], null);
            if (rowData.TryGetValue(member.DbName, out var value))
            {
                member.SetValue(value);
                args[i] = member.Value;
            }
        }

        // Invoke constructor
        return Constructor.Invoke(args);
    }

    public static object FillInstance(object inst, Dictionary<string, object?> rowData, ContractMemberDefinition[] members)
    {
        foreach (var setter in members)
        {
            var member = new ContractMember(setter, null);
            if (rowData.TryGetValue(setter.DbName, out var val)
                && member.SetFromDatasource(val, inst))
            {
                member.ApplyToEntity(inst);
            }
        }

        return inst;
    }
}