using IFY.Phorm.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormContractRunner
    {
        TResult? Get<TResult>()
            where TResult : class;
        Task<TResult?> GetAsync<TResult>(CancellationToken? cancellationToken = null)
            where TResult : class;
    }

    public interface IPhormContractRunner<T> : IPhormContractRunner
        where T : IPhormContract
    {
    }
}
