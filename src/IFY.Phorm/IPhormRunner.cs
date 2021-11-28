using IFY.Phorm.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormRunner
    {
        #region Call

        int Call(string objectName, object? args = null);
        int Call<TActionContract>(object? args = null)
            where TActionContract : IPhormContract;
        int Call<TActionContract>(TActionContract? contract) // Same as "object? args = null", but allows better Intellisense
            where TActionContract : IPhormContract;

        Task<int> CallAsync(string objectName, object? args = null, CancellationToken? cancellationToken = null);
        Task<int> CallAsync<TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TActionContract : IPhormContract;
        Task<int> CallAsync<TActionContract>(TActionContract? contract, CancellationToken? cancellationToken = null) // Same as "object? args = null", but allows better Intellisense
            where TActionContract : IPhormContract;

        #endregion Call

        #region One

        TResultContract? One<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure)
            where TResultContract : new();
        TResultContract? One<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            where TActionContract : IPhormContract;

        Task<TResultContract?> OneAsync<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure, CancellationToken? cancellationToken = null)
            where TResultContract : new();
        Task<TResultContract?> OneAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
            where TActionContract : IPhormContract;

        #endregion One

        #region Many

        TResultContract[] Many<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure)
            where TResultContract : new();
        TResultContract[] Many<TResultContract, TActionContract>(object? args = null)
            where TResultContract : new()
            where TActionContract : IPhormContract;

        Task<TResultContract[]> ManyAsync<TResultContract>(string objectName, object? args = null, DbObjectType objectType = DbObjectType.StoredProcedure, CancellationToken? cancellationToken = null)
            where TResultContract : new();
        Task<TResultContract[]> ManyAsync<TResultContract, TActionContract>(object? args = null, CancellationToken? cancellationToken = null)
            where TResultContract : new()
            where TActionContract : IPhormContract;

        #endregion Many

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
