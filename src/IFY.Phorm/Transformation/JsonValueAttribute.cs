using Newtonsoft.Json;

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
            ? JsonConvert.DeserializeObject((string)data, type, GlobalSettings.NewtonsoftJsonSerializerSettings)
            : null;
    }

    /// <inheritdoc/>
    public override object? ToDatasource(object? data, object? context)
    {
        return data != null
            ? JsonConvert.SerializeObject(data, GlobalSettings.NewtonsoftJsonSerializerSettings)
            : null;
    }
}
