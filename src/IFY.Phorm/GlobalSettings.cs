using IFY.Phorm.Encryption;

namespace IFY.Phorm
{
    public static class GlobalSettings
    {
        /// <summary>
        /// The registered instance of the <see cref="IEncryptionProvider"/> to use for secure value handling.
        /// </summary>
        public static IEncryptionProvider? EncryptionProvider { get; set; }
    }
}
