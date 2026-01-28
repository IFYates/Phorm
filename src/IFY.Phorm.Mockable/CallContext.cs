namespace IFY.Phorm.Mockable;

public class CallContext(string? connectionName, IReadOnlyDictionary<string, object?> contextData, string? targetSchema, string? targetObject, DbObjectType? objectType, int? transactionId, bool readOnly)
{
    public string? ConnectionName { get; } = connectionName;
    public IReadOnlyDictionary<string, object?> ContextData { get; } = contextData;
    public string? TargetSchema { get; } = targetSchema;
    public string? TargetObject { get; } = targetObject;
    public DbObjectType? TargetObjectType { get; } = objectType;
    public int? TransactionId { get; } = transactionId;
    public bool IsInTransaction { get; } = transactionId != null;
    public bool IsReadOnly { get; } = readOnly;
}