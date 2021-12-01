using System;
using System.Text.Json;

namespace IFY.Phorm.Transformation
{
    /// <summary>
    /// Transform the contract property object to JSON, or the datasource value from JSON.
    /// </summary>
    public class JsonValueAttribute : AbstractTransphormAttribute
    {
        // TODO: expose important JSON serializer settings

        public override object? FromDatasource(Type type, object? data)
        {
            return data != null ? JsonSerializer.Deserialize((string)data, type) : null;
        }

        public override object? ToDatasource(object? data)
        {
            return data != null ? JsonSerializer.Serialize(data) : null;
        }
    }
}
