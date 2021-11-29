using IFY.Phorm.Encryption;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("IFY.Phorm.Tests")]

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
