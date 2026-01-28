using IFY.Phorm.Data;

namespace IFY.Phorm.Mockable;

public static class PhormSessionMockExtensions
{
    /// <summary>
    /// Converts the specified mock session object to an instance of the IPhormSession interface.
    /// </summary>
    /// <remarks>Use this method to facilitate testing scenarios where an IPhormSession implementation is
    /// required, allowing mock behavior to be injected in place of a real session.</remarks>
    /// <param name="mockObject">The mock session object to convert. This parameter cannot be null.</param>
    /// <returns>An IPhormSession instance that wraps the provided mock session object.</returns>
    public static IPhormSession ToMock(this IPhormSessionMock mockObject)
    {
        return new MockPhormSession(mockObject);
    }

    /// <summary>
    /// Determines whether two objects are structurally equivalent based on the contract defined by the specified type.
    /// </summary>
    /// <remarks>This method compares the properties defined in the contract type TContract. If both objects
    /// are null for a property, they are considered equal. If any property values differ, the method returns
    /// false.</remarks>
    /// <typeparam name="TContract">The type that defines the contract for comparison, specifying the structure and properties to evaluate.</typeparam>
    /// <param name="obj1">The first object to compare, which may be null.</param>
    /// <param name="obj2">The second object to compare, which may be null.</param>
    /// <returns>true if the objects are equivalent according to the contract; otherwise, false.</returns>
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