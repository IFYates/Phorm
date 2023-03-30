namespace IFY.Phorm.Execution;

public interface IPhormFilteredContractRunner<TResult>
    where TResult : class
{
    TResult GetAll();
    public Task<TResult> GetAllAsync()
        => GetAllAsync(CancellationToken.None);
    Task<TResult> GetAllAsync(CancellationToken cancellationToken);
}
