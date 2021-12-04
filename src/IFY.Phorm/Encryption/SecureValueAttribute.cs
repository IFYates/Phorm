using System;
using System.Reflection;

namespace IFY.Phorm.Encryption
{
    /// <summary>
    /// Basic implementation of <see cref="IEncryptor"/>-based encryption of secure values.
    /// </summary>
    public class SecureValueAttribute : AbstractSecureValueAttribute
    {
        private readonly string _dataClassification;
        private readonly string? _authenticatorPropertyName;

        private object? _context;

        private static object? _lastInstance = null;
        private static string? _lastProperty = null;
        private static byte[] _lastValue = Array.Empty<byte>();

        /// <summary>
        /// This contract property represents a value that is stored encrypted.
        /// </summary>
        /// <param name="dataClassification">The classification for this value type, which can be used to control the encryption used.</param>
        /// <param name="authenticatorPropertyName">The name of the property holding the authenticator value.</param>
        public SecureValueAttribute(string dataClassification, string? authenticatorPropertyName = null)
        {
            _dataClassification = dataClassification;
            _authenticatorPropertyName = authenticatorPropertyName;
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

        public override byte[] Decrypt(byte[]? value)
        {
            if (value == null)
            {
                return Array.Empty<byte>();
            }

            if (GlobalSettings.EncryptionProvider == null)
            {
                throw new NullReferenceException($"The {nameof(GlobalSettings.EncryptionProvider)} has not not been registered.");
            }

            var encryptor = GlobalSettings.EncryptionProvider.GetInstance(_dataClassification);
            if (encryptor == null)
            {
                return value;
            }

            encryptor.Authenticator = resolveAuthenticator(_context, _authenticatorPropertyName);
            return encryptor.Decrypt(value);
        }

        public override byte[] Encrypt(object? value)
        {
            if (value == null)
            {
                return Array.Empty<byte>();
            }
            var bytes = value.GetBytes();

            if (GlobalSettings.EncryptionProvider == null)
            {
                throw new NullReferenceException($"The {nameof(GlobalSettings.EncryptionProvider)} has not not been registered.");
            }

            var encryptor = GlobalSettings.EncryptionProvider.GetInstance(_dataClassification);
            if (encryptor == null)
            {
                // TODO: Fail instead?
                return bytes;
            }

            encryptor.Authenticator = resolveAuthenticator(_context, _authenticatorPropertyName);
            return encryptor.Encrypt(bytes);
        }

        public override void SetContext(object? context)
        {
            _context = context;
            // TODO: clear _last* fields here, if doesn't break caching
        }
    }
}
