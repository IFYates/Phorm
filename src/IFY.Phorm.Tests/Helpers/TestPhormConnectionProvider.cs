using IFY.Phorm.Connectivity;
using System;

namespace IFY.Phorm.Tests
{
    internal class TestPhormConnectionProvider : IPhormDbConnectionProvider
    {
        public TestPhormConnection? TestConnection { get; } = null;

        private readonly Func<string?, IPhormDbConnection> _provider;

        public event EventHandler<IPhormDbConnection>? Connected;

        public TestPhormConnectionProvider()
        {
            TestConnection = new TestPhormConnection(null);
            _provider = (s) => TestConnection;
        }
        public TestPhormConnectionProvider(Func<string?, IPhormDbConnection> provider)
        {
            _provider = provider;
        }

        public IPhormDbConnection GetConnection(string? connectionName)
        {
            return _provider(connectionName);
        }
    }
}
