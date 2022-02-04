using System;
using Newtonsoft.Json;

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
            return data != null ? JsonConvert.DeserializeObject((string)data, type) : null;
        }

        public override object? ToDatasource(object? data)
        {
            // TODO: settings
            return data != null ? JsonConvert.SerializeObject(data) : null;
        }
    }
}
