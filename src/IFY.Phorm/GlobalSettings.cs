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
        /// The registered instance of the <see cref="IEncryptionProvider"/> to use for secure value handling.
        /// </summary>
        public static IEncryptionProvider? EncryptionProvider { get; set; }

        /// <summary>
        /// The global <see cref="JsonSerializerSettings"/> to use when dealing with JSON.
        /// If null, uses library defaults.
        /// </summary>
        public static JsonSerializerSettings? NewtonsoftJsonSerializerSettings { get; set; }

        /// <summary>
        /// Whether to throw an exception if an invocation result includes more records than expected.
        /// Defaults to true.
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static bool StrictResultSize { get; set; } = true;
    }
}
