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

    private static readonly object _lock = 1;
    private static PropertyInfo? _lastProperty;
    private static object? _lastInstance;
    private static byte[] _lastValue = [];

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
            return [];
        }
        if (_lastInstance != context)
        {
            lock (_lock)
            {
                if (_lastInstance != context)
                {
                    if (_lastProperty?.Name != propertyName
                        || _lastProperty.DeclaringType != context.GetType())
                    {
                        // Find property
                        _lastProperty = context.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                            ?? throw new InvalidOperationException($"Specified authenticator '{propertyName}' is not a public property on type {context.GetType().FullName}");
                    }

                    // Get value as byte[]
                    var value = _lastProperty.GetValue(context);
                    _lastInstance = context;
                    _lastValue = value.GetBytes();
                }
            }
        }
        return _lastValue;
    }

    /// <inheritdoc/>
    public override byte[]? Decrypt(byte[]? value, object? context)
    {
        if (value == null)
        {
            return [];
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
            return [];
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
