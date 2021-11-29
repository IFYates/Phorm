using IFY.Phorm.Data;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm
{
    public interface IPhormContractRunner
    {
        int Call(object? args = null);
        Task<int> CallAsync(object? args = null, CancellationToken? cancellationToken = null);

        TResult[] Many<TResult>(object? args = null)
            where TResult : new();
        Task<TResult[]> ManyAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null)
            where TResult : new();

        TResult? One<TResult>(object? args = null)
            where TResult : new();
        Task<TResult?> OneAsync<TResult>(object? args = null, CancellationToken? cancellationToken = null) // Same as "object? args = null", but allows better Intellisense
            where TResult : new();
    }

    public interface IPhormContractRunner<T> : IPhormContractRunner
        where T : IPhormContract
    {
        int Call(T args); // Same as "object? args = null", but allows better Intellisense
        Task<int> CallAsync(T args, CancellationToken? cancellationToken = null); // Same as "object? args = null", but allows better Intellisense

        TResult[] Many<TResult>(T args) // Same as "object? args = null", but allows better Intellisense
            where TResult : new();
        Task<TResult[]> ManyAsync<TResult>(T args, CancellationToken? cancellationToken = null) // Same as "object? args = null", but allows better Intellisense
            where TResult : new();

        TResult? One<TResult>(T args) // Same as "object? args = null", but allows better Intellisense
            where TResult : new();
        Task<TResult?> OneAsync<TResult>(T args, CancellationToken? cancellationToken = null)
            where TResult : new();
    }
}
