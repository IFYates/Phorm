using System;

namespace IFY.Phorm.Encryption
{
    /// <summary>
    /// An implementation of an encryption and decryption handler that makes no changes.
    /// </summary>
    public class NullEncryptor : IEncryptor
    {
        public byte[] Authenticator { get; set; } = Array.Empty<byte>();
        public byte[] InitialVector { get; } = Array.Empty<byte>();
        
        public byte[] Encrypt(byte[] data) => data;
        
        public byte[] Decrypt(byte[] data) => data;
    }
}
