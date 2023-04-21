using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Encryption;

/// <summary>
/// An implementation of an encryption and decryption handler that makes no changes.
/// </summary>
#if !NET5_0_OR_GREATER
[ExcludeFromCodeCoverage]
#else
[ExcludeFromCodeCoverage(Justification = "No logic")]
#endif
public class NullEncryptor : IEncryptor
{
    /// <inheritdoc/>
    public byte[] Authenticator { get => Array.Empty<byte>(); set => _ = value; /* NOOP */  }
    /// <inheritdoc/>
    public byte[] InitialVector { get => Array.Empty<byte>(); set => _ = value; /* NOOP */  }

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] data) => data;
    
    /// <inheritdoc/>
    public byte[] Decrypt(byte[] data) => data;
}
