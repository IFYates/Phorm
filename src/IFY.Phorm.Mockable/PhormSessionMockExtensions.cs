using IFY.Phorm.Data;

namespace IFY.Phorm.Mockable;

public static class PhormSessionMockExtensions
{
    public static MockPhormSession ToMock(this IPhormSessionMock mockObject)
    {
        return new MockPhormSession(mockObject);
    }

    /// <summary>
    /// 
    /// </summary>
    public static bool IsLike<TContract>(this object? obj1, object? obj2)
    {
        var defs = ContractMemberDefinition.GetFromContract(typeof(TContract));
        foreach (var def in defs)
        {
            var val1 = def.FromEntity(obj1);
            var val2 = def.FromEntity(obj2);
            if ((val1.Value == null && val2.Value == null)
                || val1.Value?.Equals(val2.Value) != true)
            {
                return false;
            }
        }
        return true;
    }
}