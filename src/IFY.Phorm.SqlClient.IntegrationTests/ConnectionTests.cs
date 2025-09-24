using IFY.Phorm.Execution;
using System.Collections.Concurrent;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ConnectionTests : SqlIntegrationTestBase
{
    public class ContextTest
    {
        public string? Context { get; set; }
        public string? SPID { get; set; }
    }

    private async Task setContextTestContract(AbstractPhormSession connProv)
    {
        await SqlTestHelpers.ApplySql(connProv, TestContext.CancellationTokenSource.Token, @"CREATE OR ALTER PROC [dbo].[usp_ContextTest]
AS
	SET NOCOUNT ON
    SELECT APP_NAME() [Context], @@SPID [SPID]
RETURN 1");
    }

    [TestMethod]
    public async Task Connection_naming_is_accessible_in_database()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");
        await setContextTestContract(phorm);

        // Act
        var res = await phorm.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual("TestContext", res!.Context);
    }

    [TestMethod]
    public async Task Multiple_connections_can_be_named_differently()
    {
        // Arrange
        var phorm1 = getPhormSession("TestContext1");
        await setContextTestContract(phorm1);

        var phorm2 = getPhormSession("TestContext2");

        var results = new ConcurrentBag<string>();

        // Act
        var t1 = Task.Run(async () =>
        {
            for (var i = 0; i < 5; ++i)
            {
                var res = await phorm1.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);
                results.Add("1:" + res!.Context);
                Thread.Sleep(i * 10);
            }
        }, TestContext.CancellationTokenSource.Token);
        var t2 = Task.Run(async () =>
        {
            for (var i = 0; i < 5; ++i)
            {
                var res = await phorm2.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);
                results.Add("2:" + res!.Context);
                Thread.Sleep((5 - i) * 10);
            }
        }, TestContext.CancellationTokenSource.Token);
        await Task.WhenAll(t1, t2);

        // Assert
        Assert.HasCount(10, results);
        Assert.AreEqual(5, results.Count(r => r == "1:TestContext1"));
        Assert.AreEqual(5, results.Count(r => r == "2:TestContext2"));
    }

    [TestMethod]
    public async Task Connection_is_reused_if_possible()
    {
        // Arrange
        var phorm1 = getPhormSession("TestContext1");
        await setContextTestContract(phorm1);

        // Act
        var res1 = await phorm1.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);

        var phorm2 = getPhormSession("TestContext1");
        var res2 = await phorm2.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);

        var phorm3 = getPhormSession("TestContext2");
        var res3 = await phorm3.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual("TestContext1", res1!.Context);
        Assert.AreEqual("TestContext1", res2!.Context);
        Assert.AreEqual("TestContext2", res3!.Context);
        Assert.AreEqual(res1.SPID, res2.SPID); // Should match (connection reuse)
        Assert.AreNotEqual(res1.SPID, res3.SPID); // Must not match (different connection)
    }

    // TODO: Too unstable to test; find a better way
    //[TestMethod]
    //public void Connection_is_not_reused_after_expiry()
    //{
    //    // Arrange
    //    var phorm1 = getPhormSession("TestContext1");
    //    setContextTestContract(phorm1);

    //    // Act
    //    var res1 = phorm1.From("ContextTest").Get<ContextTest>()!;
    //    phorm1.GetConnection().Dispose();

    //    AbstractPhormSession.ResetConnectionPool();
    //    var phorm2 = getPhormSession("TestContext1");
    //    var res2 = phorm2.From("ContextTest").Get<ContextTest>()!;

    //    // Assert
    //    Assert.AreEqual("TestContext1", res1.Context);
    //    Assert.AreEqual("TestContext1", res2.Context);
    //    Assert.AreNotEqual(res1.SPID, res2.SPID); // Should not match (reset pool)
    //}

    [TestMethod]
    public async Task Number_of_connections_does_not_increase_significantly()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, @"CREATE OR ALTER PROC [dbo].[usp_GetConnectionCount]
AS
	SET NOCOUNT ON
    DECLARE @Count INT = (SELECT COUNT(1) FROM sys.sysprocesses WHERE DB_NAME([dbid]) = DB_NAME())
RETURN @Count");

        // Act
        var res1 = await phorm.CallAsync("GetConnectionCount", null, TestContext.CancellationTokenSource.Token);
        var threads = new List<Thread>();
        for (var i = 0; i < 100; ++i)
        {
            var t = new Thread(async () =>
            {
                await getPhormSession("TestContext").CallAsync("GetConnectionCount", null, TestContext.CancellationTokenSource.Token);
            });
            threads.Add(t);
            t.Start();
        }
        var res2 = await phorm.CallAsync("GetConnectionCount", null, TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.IsLessThan(10, res2 - res1, $"First:{res1}, Last:{res2}");
    }
}