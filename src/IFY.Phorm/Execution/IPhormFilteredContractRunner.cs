namespace IFY.Phorm.Execution;

public interface IPhormFilteredContractRunner<TEntity>
    where TEntity : class, new()
{
    IEnumerable<TEntity> GetAll();
    public Task<IEnumerable<TEntity>> GetAllAsync()
        => GetAllAsync(CancellationToken.None);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken);
}
