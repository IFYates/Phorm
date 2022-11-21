namespace IFY.Phorm.Mockable;

public class CallContext
{
    public string? ConnectionName { get; }
    public string? TargetSchema { get; }
    public string? TargetObject { get; }
    public DbObjectType? ObjectType { get; }

    public CallContext(string? connectionName, string? targetSchema, string? targetObject, DbObjectType objectType)
    {
        ConnectionName = connectionName;
        TargetSchema = targetSchema;
        TargetObject = targetObject;
        ObjectType = objectType;
    }
}
