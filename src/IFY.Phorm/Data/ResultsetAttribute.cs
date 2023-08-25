namespace IFY.Phorm.Data;

/// <summary>
/// Marks the property as being the target of an additional resultset.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ResultsetAttribute : Attribute
{
    /// <summary>
    /// The 0-based index of the additional resultset to match.
    /// </summary>
    public int Order { get; }
    /// <summary>
    /// The name of the sibling property that provides the <see cref="IRecordMatcher"/> implementation for matching the resultset records to the parent instances.
    /// </summary>
    public string? SelectorPropertyName { get; }

    private Type? _lastSelectorType;
    private IRecordMatcher? _lastMatcher;

    public ResultsetAttribute(int order)
    {
        Order = order;
    }
    public ResultsetAttribute(int order, string selectorPropertyName)
    {
        Order = order;
        SelectorPropertyName = selectorPropertyName;
    }

    internal object[] FilterMatched(object parent, IEnumerable<object> children)
    {
        // No selector means take all children
        if (SelectorPropertyName == null)
        {
            return children.ToArray();
        }

        // Cache the selector temporarily, as we often use the same one in batches
        if (_lastSelectorType != parent.GetType())
        {
            var selectorProp = parent.GetType().GetProperty(SelectorPropertyName);
#if !NET5_0_OR_GREATER
            if (selectorProp?.PropertyType == null || !typeof(IRecordMatcher).IsAssignableFrom(selectorProp.PropertyType))
#else
            if (selectorProp?.PropertyType.IsAssignableTo(typeof(IRecordMatcher)) != true)
#endif
            {
                throw new InvalidCastException($"Selector property '{SelectorPropertyName}' does not return IRecordMatcher.");
            }

            _lastSelectorType = parent.GetType();
            _lastMatcher = selectorProp.GetAccessors()[0].IsStatic
                ? (IRecordMatcher?)selectorProp.GetValue(null)
                : (IRecordMatcher?)selectorProp.GetValue(parent);
        }
        return children.Where(c => _lastMatcher!.IsMatch(parent, c) == true).ToArray();
    }
}
