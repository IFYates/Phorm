using IFY.Phorm.Encryption;
using Newtonsoft.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.Mockable")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.SqlClient.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.SqlClient.IntegrationTests")]

namespace IFY.Phorm;

public static class GlobalSettings
{
    /// <summary>
    /// The registered instance of the <see cref="IEncryptionProvider"/> to use for secure value handling.
    /// </summary>
    public static IEncryptionProvider? EncryptionProvider { get; set; }

    /// <summary>
    /// If true, will consume execution errors and treat like a console message.
    /// Defaults to false.
    /// </summary>
    public static bool ExceptionsAsConsoleMessage { get; set; }

    /// <summary>
    /// The global <see cref="JsonSerializerSettings"/> to use when dealing with JSON.
    /// If null, uses library defaults.
    /// </summary>
    public static JsonSerializerSettings? NewtonsoftJsonSerializerSettings { get; set; }

    /// <summary>
    /// Whether to throw a <see cref="System.InvalidOperationException"/> if an invocation result includes more records than expected.
    /// Defaults to true.
    /// </summary>
    public static bool StrictResultSize { get; set; } = true;

    public static string ProcedurePrefix
    {
        get;
        set;
    } = "usp_";
    public static string TablePrefix
    {
        get;
        set;
    } = string.Empty;
    public static string ViewPrefix
    {
        get;
        set;
    } = "vw_";
}
