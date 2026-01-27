namespace IFY.Phorm.Data;

/// <summary>
/// Defines a method for determining whether a child record matches a parent record according to custom criteria.
/// </summary>
/// <remarks>Implementations of this interface can be used to compare records in scenarios such as data
/// synchronization, merging, or filtering. The matching logic is defined by the implementer and may vary depending on
/// the application's requirements.</remarks>
public interface IRecordMatcher
{
    /// <summary>
    /// Determines whether the specified child object matches the criteria defined by the parent object.
    /// </summary>
    /// <param name="parent">The object that defines the criteria or context against which the child object is evaluated. Cannot be null.</param>
    /// <param name="child">The object to be tested for a match against the parent criteria. Cannot be null.</param>
    /// <returns>true if the child object matches the criteria of the parent object; otherwise, false.</returns>
    bool IsMatch(object parent, object child);
}

/// <summary>
/// Provides a generic mechanism for determining whether a parent and child record match according to a user-defined
/// predicate.
/// </summary>
/// <typeparam name="TParent">The type of the parent record to be compared. Must be a reference type.</typeparam>
/// <typeparam name="TChild">The type of the child record to be compared. Must be a reference type.</typeparam>
/// <param name="matcher">A function that defines the matching logic between a parent and child record. The function should return <see
/// langword="true"/> if the records match; otherwise, <see langword="false"/>.</param>
public class RecordMatcher<TParent, TChild>(Func<TParent, TChild, bool> matcher) : IRecordMatcher
    where TParent : class
    where TChild : class
{
    /// <inheritdoc/>
    bool IRecordMatcher.IsMatch(object parent, object child)
    {
        var typedParent = parent as TParent ?? throw new InvalidCastException($"Parent entity type '{parent.GetType().FullName}' could not be used for matcher expecting type '{typeof(TParent).FullName}'.");
        var typedChild = child as TChild ?? throw new InvalidCastException($"Child entity type '{child.GetType().FullName}' could not be used for matcher expecting type '{typeof(TChild).FullName}'.");
        return IsMatch(typedParent, typedChild);
    }

    /// <summary>
    /// Determines whether the specified parent and child objects satisfy the matching criteria defined by the matcher.
    /// </summary>
    /// <param name="parent">The parent object to evaluate against the matching criteria.</param>
    /// <param name="child">The child object to evaluate against the matching criteria.</param>
    /// <returns>true if the parent and child objects match according to the criteria; otherwise, false.</returns>
    public bool IsMatch(TParent parent, TChild child)
    {
        return matcher(parent, child);
    }
}