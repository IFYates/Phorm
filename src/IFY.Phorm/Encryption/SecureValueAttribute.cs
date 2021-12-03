using System;
using System.Reflection;
using System.Text;

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

        // TODO: more generic helper
        private static byte[] getBytes(object value)
        {
            if (value is decimal dec)
            {
                var ints = Decimal.GetBits(dec);
                var bytes = new byte[16];
                Array.Copy(BitConverter.GetBytes(ints[0]), 0, bytes, 0, 4);
                Array.Copy(BitConverter.GetBytes(ints[1]), 0, bytes, 4, 4);
                Array.Copy(BitConverter.GetBytes(ints[2]), 0, bytes, 8, 4);
                Array.Copy(BitConverter.GetBytes(ints[3]), 0, bytes, 12, 4);
                return bytes;
            }

            return value switch
            {
                byte[] val => val,
                byte val => BitConverter.GetBytes(val),
                char val => BitConverter.GetBytes(val),
                double val => BitConverter.GetBytes(val),
                float val => BitConverter.GetBytes(val),
                Guid val => val.ToByteArray(),
                int val => BitConverter.GetBytes(val),
                long val => BitConverter.GetBytes(val),
                short val => BitConverter.GetBytes(val),
                string val => Encoding.UTF8.GetBytes(val),
                _ => throw new InvalidCastException(),
            };
        }

        private static byte[] resolveAuthenticator(object? context, string? propertyName)
        {
            if (context != null && _lastInstance != context && propertyName != null)
            {
                var authenticatorProperty = context.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new InvalidOperationException($"Specified authenticator '{propertyName}' is not a public property on type {context.GetType().FullName}");
                _lastInstance = context;
                _lastValue = Array.Empty<byte>();

                // Get value as byte[]
                var value = authenticatorProperty.GetValue(context);
                if (value != null)
                {
                    _lastValue = getBytes(value);
                }
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
            var bytes = getBytes(value);

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
        }
    }
}
