namespace IFY.Phorm.Execution;

public interface IPhormFilteredContractRunner<TEntity>
    where TEntity : class, new()
{
    IEnumerable<TEntity> GetAll();
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken);
}
