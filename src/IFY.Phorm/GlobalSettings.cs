using IFY.Phorm.Encryption;
using Newtonsoft.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.SqlClient.Tests")]

namespace IFY.Phorm;

/// <summary>
/// Provides global configuration settings for encryption, error handling, JSON serialization, and database object
/// naming conventions used throughout the application.
/// </summary>
/// <remarks>This class contains static properties that control application-wide behaviors, such as the encryption
/// provider for secure value handling, error reporting preferences, JSON serialization settings, and default prefixes
/// for stored procedures, tables, and views. Changes to these settings affect all components that reference them.
/// Thread safety is not guaranteed; ensure appropriate synchronization if settings are modified at runtime.</remarks>
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
    /// Whether to throw a <see cref="InvalidOperationException"/> if an invocation result includes more records than expected.
    /// Defaults to true.
    /// </summary>
    public static bool StrictResultSize { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefix used for accessing stored procedures in the database.
    /// </summary>
    /// <remarks>The default value is "usp_". This property can be set to customize the naming convention for
    /// stored procedures in database operations.</remarks>
    public static string ProcedurePrefix
    {
        get;
        set;
    } = "usp_";
    /// <summary>
    /// Gets or sets the prefix to use for accessing database tables.
    /// </summary>
    public static string TablePrefix
    {
        get;
        set;
    } = string.Empty;
    /// <summary>
    /// Gets or sets the prefix used for identifying database views.
    /// </summary>
    /// <remarks>The default value is "vw_". This property can be set to customize the naming convention for
    /// views in database operations.</remarks>
    public static string ViewPrefix
    {
        get;
        set;
    } = "vw_";
}
