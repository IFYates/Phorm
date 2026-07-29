using System.Text.Json;

namespace IFY.Phorm.Transformation;

/// <summary>
/// Transform the contract property object to JSON, or the datasource value from JSON.
/// </summary>
public class JsonValueAttribute : AbstractTransphormAttribute
{
    /// <inheritdoc/>
    public override object? FromDatasource(Type type, object? data, object? context)
    {
        return data != null
            ? JsonSerializer.Deserialize((string)data, type, GlobalSettings.JsonSerializerOptions)
            : null;
    }

    /// <inheritdoc/>
    public override object? ToDatasource(object? data, object? context)
    {
        return data != null
            ? JsonSerializer.Serialize(data, GlobalSettings.JsonSerializerOptions)
            : null;
    }
}
