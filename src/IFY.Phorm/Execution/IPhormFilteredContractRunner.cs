namespace IFY.Phorm.Execution;

public interface IPhormFilteredContractRunner<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// The resolve entity type.
    /// </summary>
    public Type EntityType => typeof(TEntity);

    IEnumerable<TEntity> GetAll();
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken);
}
