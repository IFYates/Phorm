namespace IFY.Phorm.Encryption;

/// <summary>
/// Provides factory method for getting the required encryption handler.
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Returns the appropriate implementation of <see cref="IEncryptor"/> that can be used to decrypt data classified as <paramref name="dataClassification"/>.
    /// Can use the <paramref name="data"/> to be decrypted to pick the most appropriate implementation.
    /// </summary>
    /// <param name="dataClassification">The data classification of the value to be decrypted.</param>
    /// <param name="data">The data that will need to be decrypted by the instance.</param>
    /// <returns>The <see cref="IEncryptor"/> instance that will be used to decrypt the data.</returns>
    IEncryptor GetDecryptor(string dataClassification, byte[] data);

    /// <summary>
    /// Returns the appropriate implementation of <see cref="IEncryptor"/> that can be used to encrypt data classified as <paramref name="dataClassification"/>.
    /// </summary>
    /// <param name="dataClassification">The data classification of the value to be encrypted.</param>
    /// <returns>The <see cref="IEncryptor"/> instance that will be used to encrypt the data.</returns>
    IEncryptor GetEncryptor(string dataClassification);
}
