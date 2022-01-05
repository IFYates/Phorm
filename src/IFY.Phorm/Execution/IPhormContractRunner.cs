using IFY.Phorm.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormContractRunner
    {
        /// <summary>
        /// Get one or more entity instances.
        /// </summary>
        /// <typeparam name="TResult">The type of entity to map result data to.</typeparam>
        /// <returns>When <typeparamref name="TResult"/> is the entity type, will return the single result instance or null. When <typeparamref name="TResult"/> is an array of the entity type, will return an array of all types from the result (never null).</returns>
        TResult? Get<TResult>()
            where TResult : class;
        /// <summary>
        /// Get one or more entity instances.
        /// </summary>
        /// <typeparam name="TResult">The type of entity to map result data to.</typeparam>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>When <typeparamref name="TResult"/> is the entity type, will return the single result instance or null. When <typeparamref name="TResult"/> is an array of the entity type, will return an array of all types from the result (never null).</returns>
        Task<TResult?> GetAsync<TResult>(CancellationToken? cancellationToken = null)
            where TResult : class;
    }

    public interface IPhormContractRunner<T> : IPhormContractRunner
        where T : IPhormContract
    {
    }
}
