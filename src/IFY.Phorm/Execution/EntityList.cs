using System.Collections;

namespace IFY.Phorm.Execution;

internal interface IEntityList : IEnumerable
{
    int Count { get; }
    void AddResolver(Func<object> resolver);
}

internal class EntityList<TEntity> : IEntityList, IEnumerable<TEntity>, ICollection<TEntity>
{
    private readonly Queue<Func<TEntity>> _resolvers = new();
    private readonly List<TEntity> _entities = [];

    public int Count => _entities.Count + _resolvers.Count;

    public bool IsReadOnly => true;

    public void AddResolver(Func<object> resolver)
        => AddResolver(() => (TEntity)resolver());
    public void AddResolver(Func<TEntity> resolver)
    {
        _resolvers.Enqueue(resolver);
    }

    public bool Contains(TEntity item)
    {
        // Can't be in unresolved list, so only check current
        return _entities.Contains(item);
    }

    public void CopyTo(TEntity[] array, int arrayIndex)
    {
        using var enumerator = GetEnumerator();
        for (var i = arrayIndex; i < array.Length && enumerator.MoveNext(); ++i)
        {
            array[i] = enumerator.Current;
        }
    }

    public IEnumerator<TEntity> GetEnumerator()
    {
        // Iterate resolved entities
        foreach (var entity in _entities)
        {
            yield return entity;
        }

        // Resolve unresolved entities
        while (_resolvers.Count > 0)
        {
            var entity = _resolvers!.Dequeue().Invoke();
            _entities.Add(entity);
            yield return entity;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(TEntity item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Remove(TEntity item)
    {
        throw new NotImplementedException();
    }
}