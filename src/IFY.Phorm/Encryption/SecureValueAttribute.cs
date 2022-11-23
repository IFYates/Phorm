using System.Reflection;

namespace IFY.Phorm.Encryption;

/// <summary>
/// Basic implementation of <see cref="IEncryptor"/>-based encryption of secure values.
/// </summary>
public class SecureValueAttribute : AbstractSecureValueAttribute
{
    /// <summary>
    /// The identifier for the classification of the data, used to determine the encryption/decryption implementation.
    /// </summary>
    public string DataClassification { get; }
    /// <summary>
    /// The name of a property on this contract/DTO that is used as an encryption/decryption authenticator.
    /// </summary>
    public string? AuthenticatorPropertyName { get; }

    private static object? _lastInstance;
    private static string? _lastProperty;
    private static byte[] _lastValue = Array.Empty<byte>();

    /// <summary>
    /// This contract property represents a value that is stored encrypted.
    /// </summary>
    /// <param name="dataClassification">The classification for this value type, which can be used to control the encryption used.</param>
    public SecureValueAttribute(string dataClassification)
    {
        DataClassification = dataClassification;
    }
    /// <summary>
    /// This contract property represents a value that is stored encrypted.
    /// </summary>
    /// <param name="dataClassification">The classification for this value type, which can be used to control the encryption used.</param>
    /// <param name="authenticatorPropertyName">The name of the property holding the authenticator value.</param>
    public SecureValueAttribute(string dataClassification, string? authenticatorPropertyName)
    {
        DataClassification = dataClassification;
        AuthenticatorPropertyName = authenticatorPropertyName;
    }

    private static byte[] resolveAuthenticator(object? context, string? propertyName)
    {
        if (context == null || propertyName == null)
        {
            return Array.Empty<byte>();
        }
        if (_lastInstance != context || _lastProperty != propertyName)
        {
            // Find property
            var authenticatorProperty = context.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Specified authenticator '{propertyName}' is not a public property on type {context.GetType().FullName}");

            // Remember this state
            _lastInstance = context;
            _lastProperty = propertyName;

            // Get value as byte[]
            var value = authenticatorProperty.GetValue(context);
            _lastValue = value.GetBytes();
        }
        return _lastValue;
    }

    /// <inheritdoc/>
    public override byte[]? Decrypt(byte[]? value, object? context)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }

        if (GlobalSettings.EncryptionProvider == null)
        {
            throw new InvalidOperationException($"The {nameof(GlobalSettings.EncryptionProvider)} has not not been registered.");
        }

        var encryptor = GlobalSettings.EncryptionProvider.GetDecryptor(DataClassification, value);
        if (encryptor == null)
        {
            // TODO: Fail instead?
            return value;
        }

        encryptor.Authenticator = resolveAuthenticator(context, AuthenticatorPropertyName);
        return encryptor.Decrypt(value);
    }

    /// <inheritdoc/>
    public override byte[] Encrypt(object? value, object? context)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }
        var bytes = value.GetBytes();

        if (GlobalSettings.EncryptionProvider == null)
        {
            throw new InvalidOperationException($"The {nameof(GlobalSettings.EncryptionProvider)} has not not been registered.");
        }

        var encryptor = GlobalSettings.EncryptionProvider.GetEncryptor(DataClassification);
        if (encryptor == null)
        {
            // TODO: Fail instead?
            return bytes;
        }

        encryptor.Authenticator = resolveAuthenticator(context, AuthenticatorPropertyName);
        return encryptor.Encrypt(bytes);
    }
}
