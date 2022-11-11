namespace IFY.Phorm.Data;

/// <summary>
/// Mark this "Specialised" type with the details on how to differentiate it within the "Generalised" data.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PhormSpecOfAttribute : Attribute
{
    /// <summary>
    /// The name of the property on the "Generalised" type definition to use when matching to a "Specialised" type.
    /// </summary>
    public string GenProperty { get; }
    /// <summary>
    /// The value of the "Generalised" type property to match with this "Specialised" type.
    /// </summary>
    public object PropertyValue { get; }

    public PhormSpecOfAttribute(string genProperty, object propertyValue)
    {
        GenProperty = genProperty;
        PropertyValue = propertyValue ?? throw new ArgumentNullException(nameof(propertyValue));
    }
}
