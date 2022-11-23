namespace IFY.Phorm.Encryption;

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
    /// <param name="data">The data to be decrypted.</param>
    /// <returns>The decrypted value as bytes.</returns>
    byte[]? Decrypt(byte[] data);

    /// <summary>
    /// Encrypt the value bytes using this implementation.
    /// </summary>
    /// <param name="value">The value (as bytes) to be encrypted.</param>
    /// <returns>The encrypted data.</returns>
    byte[] Encrypt(byte[] value);
}
