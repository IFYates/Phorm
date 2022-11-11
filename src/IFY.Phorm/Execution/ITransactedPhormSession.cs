namespace IFY.Phorm.Execution;

/// <summary>
/// A <see cref="IPhormSession"/> that is in a transaction.
/// Disposing this runner before invoking <see cref="Commit"/> is the same as calling <see cref="Rollback"/>.
/// </summary>
public interface ITransactedPhormSession : IPhormSession, IDisposable
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
