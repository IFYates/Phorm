using System;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Encryption
{
    /// <summary>
    /// An implementation of an encryption and decryption handler that makes no changes.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "No logic")]
    public class NullEncryptor : IEncryptor
    {
        public byte[] Authenticator { get; set; } = Array.Empty<byte>();
        public byte[] InitialVector { get; } = Array.Empty<byte>();
        
        public byte[] Encrypt(byte[] data) => data;
        
        public byte[] Decrypt(byte[] data) => data;
    }
}
