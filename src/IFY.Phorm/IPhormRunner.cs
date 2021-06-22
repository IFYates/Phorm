using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormRunner
    {
        #region Call

        int Call(string objectName, object? args = null);
        int Call<TActionContract>(object? args = null);
        int Call<TActionContract>(TActionContract? contract); // Same as "object? args = null", but allows better Intellisense
        
        Task<int> CallAsync(string objectName, object? args = null, CancellationToken? cancellationToken = null);
        Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null);
        Task<int> CallAsync<TActionContract>(TActionContract? contract, CancellationToken? cancellationToken = null); // Same as "object? args = null", but allows better Intellisense

        #endregion Call
        
        #region Single
        
        TResultContract? Single<TResultContract>(string objectName, object? args = null)
            where TResultContract : new();
        TResultContract? Single<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new();

        Task<TResultContract?> SingleAsync<TResultContract>(string objectName, object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new();
        Task<TResultContract?> SingleAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new();

        #endregion Single

        #region All
        
        TResultContract[] All<TResultContract>(string objectName, object? args = null)
            where TResultContract : new();
        TResultContract[] All<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new();

        Task<TResultContract[]> AllAsync<TResultContract>(string objectName, object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new();
        Task<TResultContract[]> AllAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new();

        #endregion All

        #region Transactions

        /// <summary>
        /// True if this runner implementation supports transactions.
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// True if this runner is currently in a transaction.
        /// </summary>
        bool IsInTransaction { get; }

        /// <summary>
        /// Begin a new transaction, with associated runner.
        /// </summary>
        /// <returns>The runner of the transaction.</returns>
        ITransactedPhormRunner BeginTransaction();

        #endregion Transactions
    }
}
