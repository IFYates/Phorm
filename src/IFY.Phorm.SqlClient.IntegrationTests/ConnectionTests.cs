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
        await SqlTestHelpers.ApplySql(connProv, TestContext.CancellationToken, @"CREATE OR ALTER PROC [dbo].[usp_ContextTest]
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
        var res = await phorm.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationToken);

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
                var res = await phorm1.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationToken);
                results.Add("1:" + res!.Context);
                Thread.Sleep(i * 10);
            }
        }, TestContext.CancellationToken);
        var t2 = Task.Run(async () =>
        {
            for (var i = 0; i < 5; ++i)
            {
                var res = await phorm2.From("ContextTest", null).GetAsync<ContextTest>(TestContext.CancellationToken);
                results.Add("2:" + res!.Context);
                Thread.Sleep((5 - i) * 10);
            }
        }, TestContext.CancellationToken);
        await Task.WhenAll(t1, t2);

        // Assert
        Assert.HasCount(10, results);
        Assert.AreEqual(5, results.Count(r => r == "1:TestContext1"));
        Assert.AreEqual(5, results.Count(r => r == "2:TestContext2"));
    }

    // NOTE: Low-value test; we have no control over the ADO.NET pool
    [TestMethod]
    public async Task Requests_done_on_different_sessions()
    {
        // Arrange
        var phorm = getPhormSession("TestContext1");
        await setContextTestContract(phorm);

        // Act
        var res1 = await phorm.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);
        var res2 = await phorm.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);
        var res3 = await phorm.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);

        // Assert
        Assert.AreNotEqual(res1!.SPID, res2!.SPID);
        Assert.AreNotEqual(res1.SPID, res3!.SPID);
    }

    [TestMethod]
    public async Task Transaction_requests_done_in_same_session()
    {
        // Arrange
        var phorm = getPhormSession();
        await setContextTestContract(phorm);

        // Act
        ContextTest? res1, res2, res3;
        using (var transaction = await phorm.BeginTransactionAsync(TestContext.CancellationToken))
        {
            res1 = await transaction.From("ContextTest", null)
                .GetAsync<ContextTest>(TestContext.CancellationToken);
            res2 = await transaction.From("ContextTest", null)
                .GetAsync<ContextTest>(TestContext.CancellationToken);
            res3 = await transaction.From("ContextTest", null)
                .GetAsync<ContextTest>(TestContext.CancellationToken);
        }

        // Assert
        Assert.AreEqual(res1!.SPID, res2!.SPID);
        Assert.AreEqual(res1.SPID, res3!.SPID);
    }

    // NOTE: Low-value test; we have no control over the ADO.NET pool
    [TestMethod]
    public async Task ConnectionName_forces_new_connection()
    {
        // Arrange
        var phorm1 = getPhormSession("TestContext1");
        await setContextTestContract(phorm1);

        // Act
        var res1 = await phorm1.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);

        var phorm2 = getPhormSession("TestContext1");
        var res2 = await phorm2.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);

        var phorm3 = getPhormSession("TestContext2");
        var res3 = await phorm3.From("ContextTest", null)
            .GetAsync<ContextTest>(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual("TestContext1", res1!.Context);
        Assert.AreEqual("TestContext1", res2!.Context);
        Assert.AreEqual("TestContext2", res3!.Context);
        Assert.AreNotEqual(res1.SPID, res2.SPID);
        Assert.AreNotEqual(res1.SPID, res3.SPID);
    }
}