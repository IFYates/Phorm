using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IFY.Phorm.Mockable
{
    public interface IPhormSessionMock
    {
        TResult? GetFrom<TActionContract, TResult>(object? args = null)
            where TActionContract : IPhormContract;
    }

    public class MockPhormSession : IPhormSession
    {
        private readonly IPhormSessionMock _mockObject;

        // Call
        // - name, contract, anon name
        // - null, contract, anon args
        //
        // From<T>
        // - null, contract, anon args
        // - object comparer for both
        // 
        // don't treat Async/non different (opt. strict?)
        //
        // Easy check for target Table / View / SProc
        //
        // Default is to provide verbatim result, but way to provide "raw" data that will be processed like from datasource (i.e., through attributes)
        //

        public string? ConnectionName => throw new NotImplementedException();

        public bool ExceptionsAsConsoleMessage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool StrictResultSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool SupportsTransactions => throw new NotImplementedException();

        public bool IsInTransaction => throw new NotImplementedException();

        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<CommandExecutingEventArgs> CommandExecuting;
        public event EventHandler<CommandExecutedEventArgs> CommandExecuted;
        public event EventHandler<UnexpectedRecordColumnEventArgs> UnexpectedRecordColumn;
        public event EventHandler<UnresolvedContractMemberEventArgs> UnresolvedContractMember;
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage;

        public MockPhormSession(IPhormSessionMock mockObject)
        {
            _mockObject = mockObject;
        }

        public ITransactedPhormSession BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public int Call(string contractName)
        {
            throw new NotImplementedException();
        }

        public int Call(string contractName, object? args)
        {
            throw new NotImplementedException();
        }

        public int Call<TActionContract>() where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public int Call<TActionContract>(object? args) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public int Call<TActionContract>(TActionContract contract) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync(string contractName)
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync(string contractName, object? args)
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync(string contractName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync(string contractName, object? args, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>() where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>(object? args) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>(CancellationToken cancellationToken) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>(object? args, CancellationToken cancellationToken) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>(TActionContract contract) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public Task<int> CallAsync<TActionContract>(TActionContract contract, CancellationToken cancellationToken) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public class MockPhormContractRunner<TActionContract> : IPhormContractRunner<TActionContract>
            where TActionContract : IPhormContract
        {
            private readonly IPhormSessionMock _mockObject;
            private readonly object? _args;

            public MockPhormContractRunner(IPhormSessionMock mockObject, object? args)
            {
                _mockObject = mockObject;
                _args = args;
            }

            public TResult? Get<TResult>()
                where TResult : class
            {
                return _mockObject.GetFrom<TActionContract, TResult>(_args);
            }

            public Task<TResult?> GetAsync<TResult>() where TResult : class
            {
                throw new NotImplementedException();
            }

            public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken) where TResult : class
            {
                throw new NotImplementedException();
            }
        }

        public IPhormContractRunner From(string contractName)
        {
            throw new NotImplementedException();
        }

        public IPhormContractRunner From(string contractName, object? args)
        {
            throw new NotImplementedException();
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>() where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>(object? args)
            where TActionContract : IPhormContract
        {
            return new MockPhormContractRunner<TActionContract>(_mockObject, args);
        }

        public IPhormContractRunner<TActionContract> From<TActionContract>(TActionContract contract) where TActionContract : IPhormContract
        {
            throw new NotImplementedException();
        }

        public TResult? Get<TResult>() where TResult : class
        {
            throw new NotImplementedException();
        }

        public TResult? Get<TResult>(object? args) where TResult : class
        {
            throw new NotImplementedException();
        }

        public TResult? Get<TResult>(TResult args) where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>() where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>(object? args) where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>(CancellationToken cancellationToken) where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>(object? args, CancellationToken cancellationToken) where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>(TResult args) where TResult : class
        {
            throw new NotImplementedException();
        }

        public Task<TResult?> GetAsync<TResult>(TResult args, CancellationToken cancellationToken) where TResult : class
        {
            throw new NotImplementedException();
        }

        public IPhormSession SetConnectionName(string connectionName)
        {
            throw new NotImplementedException();
        }
    }
}
