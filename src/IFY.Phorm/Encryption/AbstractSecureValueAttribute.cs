using IFY.Phorm.Data;
using System;

namespace IFY.Phorm.Encryption
{
    /// <summary>
    /// Attribute for marking a contract property as secure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class AbstractSecureValueAttribute : Attribute, IContractMemberAttribute
    {
        /// <summary>
        /// Decrypt the bytes using this implementation.
        /// </summary>
        /// <param name="value">The data to be decrypted.</param>
        /// <returns>The decrypted value.</returns>
        public abstract byte[] Decrypt(byte[]? value);
        
        /// <summary>
        /// Encrypt the value using this implementation.
        /// </summary>
        /// <param name="value">The value to be encrypted.</param>
        /// <returns>The encrypted data.</returns>
        public abstract byte[] Encrypt(object? value);

        public virtual void SetContext(object? context) { }
    }
}
