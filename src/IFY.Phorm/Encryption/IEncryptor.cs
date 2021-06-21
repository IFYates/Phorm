namespace IFY.Phorm.Encryption
{
    /// <summary>
    /// The implementation of an encryption and decryption handler.
    /// </summary>
    public interface IEncryptor
    {
        /// <summary>
        /// The additional authenticator value to use, if supported by the implementation.
        /// </summary>
        byte[] Authenticator { get; set; }
        /// <summary>
        /// The initial data vector to use, if supported by the implementation.
        /// </summary>
        byte[] InitialVector { get; }
        
        /// <summary>
        /// Decrypt the bytes using this implementation.
        /// </summary>
        /// <param name="value">The data to be decrypted.</param>
        /// <param name="instance">The contract instance that this action is related to.</param>
        /// <returns>The decrypted value.</returns>
        byte[] Encrypt(byte[] data);
        
        /// <summary>
        /// Encrypt the value using this implementation.
        /// </summary>
        /// <param name="value">The value to be encrypted.</param>
        /// <param name="instance">The contract instance that this action is related to.</param>
        /// <returns>The encrypted data.</returns>
        byte[] Decrypt(byte[] data);
    }
}
