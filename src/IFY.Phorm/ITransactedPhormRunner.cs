using System;

namespace IFY.Phorm
{
    /// <summary>
    /// A <see cref="IPhormRunner"/> that is in a transaction.
    /// Disposing this runner before invoking <see cref="Commit"/> is the same as calling <see cref="Rollback"/>.
    /// </summary>
    public interface ITransactedPhormRunner : IPhormRunner, IDisposable
    {
        /// <summary>
        /// Commit the transaction, making this runner unusable for further calls.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollback the transaction, making this runner unusable for further calls.
        /// </summary>
        void Rollback();
    }
}
