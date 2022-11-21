using IFY.Phorm.Data;

namespace IFY.Phorm.Mockable;

/// <summary>
/// Mock <see cref="IPhormSession"/> interface to make testing easier.
/// </summary>
public interface IPhormSessionMock
{
    int Call(string contractName, object? args, CallContext context);
    int Call<TActionContract>(object? args, CallContext context);
    TResult? GetFrom<TResult>(string? contractName, object? args, CallContext context);
    TResult? GetFrom<TActionContract, TResult>(object? args, CallContext context)
        where TActionContract : IPhormContract;
}
