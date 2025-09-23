namespace IFY.Phorm.Execution;

/// <summary>
/// Defines a contract for retrieving filtered results from a Phorm contract runner, supporting both synchronous and
/// asynchronous operations.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the contract runner. Must be a reference type.</typeparam>
public interface IPhormFilteredContractRunner<TResult>
    where TResult : class
{
    /// <summary>
    /// Asynchronously retrieves all results of the operation.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains all retrieved results.</returns>
    public Task<TResult> GetAllAsync()
        => GetAllAsync(CancellationToken.None);
    /// <summary>
    /// Asynchronously retrieves all items of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains all items of type <typeparamref
    /// name="TResult"/>.</returns>
    Task<TResult> GetAllAsync(CancellationToken cancellationToken);
}
