using IFY.Phorm.Encryption;
using Newtonsoft.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.SqlClient.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.SqlClient.IntegrationTests")]

namespace IFY.Phorm
{
    public static class GlobalSettings
    {
        /// <summary>
        /// The global <see cref="JsonSerializerSettings"/> to use when dealing with JSON.
        /// If null, uses library defaults.
        /// </summary>
        public static JsonSerializerSettings? NewtonsoftJsonSerializerSettings { get; set; }

        /// <summary>
        /// The registered instance of the <see cref="IEncryptionProvider"/> to use for secure value handling.
        /// </summary>
        public static IEncryptionProvider? EncryptionProvider { get; set; }
    }
}
