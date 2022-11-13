namespace IFY.Phorm.Encryption;

/// <summary>
/// Provides factory method for getting the required encryption handler.
/// </summary>
public interface IEncryptionProvider
{
    IEncryptor GetInstance(string dataClassification);
}
