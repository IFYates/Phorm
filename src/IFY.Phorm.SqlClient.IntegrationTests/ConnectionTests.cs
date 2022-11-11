using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ConnectionTests : SqlIntegrationTestBase
{
    public class ContextTest
    {
        public string? Context { get; set; }
        public string? SPID { get; set; }
    }

    private static void setContextTestContract(AbstractPhormSession connProv)
    {
        SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_ContextTest]
AS
	SET NOCOUNT ON
    SELECT APP_NAME() [Context], @@SPID [SPID]
RETURN 1");
    }

    [TestMethod]
    public void Connection_naming_is_accessible_in_database()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");
        setContextTestContract(phorm);

        // Act
        var res = phorm.From("ContextTest").Get<ContextTest>()!;

        // Assert
        Assert.AreEqual("TestContext", res.Context);
    }

    [TestMethod]
    public void Multiple_connections_can_be_named_differently()
    {
        // Arrange
        var phorm1 = getPhormSession("TestContext1");
        setContextTestContract(phorm1);

        var phorm2 = getPhormSession("TestContext2");

        var results = new List<string>();

        // Act
        var t1 = Task.Run(() =>
        {
            for (var i = 0; i < 5; ++i)
            {
                var res = phorm1.From("ContextTest").Get<ContextTest>()!;
                lock (results)
                {
                    results.Add("1:" + res.Context);
                }

                Thread.Sleep(i * 10);
            }
        });
        var t2 = Task.Run(() =>
        {
            for (var i = 0; i < 5; ++i)
            {
                var res = phorm2.From("ContextTest").Get<ContextTest>()!;
                lock (results)
                {
                    results.Add("2:" + res.Context);
                }

                Thread.Sleep((5 - i) * 10);
            }
        });
        Task.WaitAll(t1, t2);

        // Assert
        Assert.AreEqual(10, results.Count);
        Assert.AreEqual(5, results.Count(r => r == "1:TestContext1"));
        Assert.AreEqual(5, results.Count(r => r == "2:TestContext2"));
    }

    [TestMethod]
    public void Connection_is_reused_if_possible()
    {
        // Arrange
        var phorm1 = getPhormSession("TestContext1");
        setContextTestContract(phorm1);

        // Act
        var res1 = phorm1.From("ContextTest").Get<ContextTest>()!;

        var phorm2 = getPhormSession("TestContext1");
        var res2 = phorm2.From("ContextTest").Get<ContextTest>()!;

        var phorm3 = getPhormSession("TestContext2");
        var res3 = phorm3.From("ContextTest").Get<ContextTest>()!;

        // Assert
        Assert.AreEqual("TestContext1", res1.Context);
        Assert.AreEqual("TestContext1", res2.Context);
        Assert.AreEqual("TestContext2", res3.Context);
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
    public void Number_of_connections_does_not_increase_significantly()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");

        SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_GetConnectionCount]
AS
	SET NOCOUNT ON
    DECLARE @Count INT = (SELECT COUNT(1) FROM sys.sysprocesses WHERE DB_NAME([dbid]) = DB_NAME())
RETURN @Count");

        // Act
        var res1 = phorm.Call("GetConnectionCount");
        for (var i = 0; i < 100; ++i)
        {
            var t = new Thread(() =>
            {
                _ = getPhormSession("TestContext").Call("GetConnectionCount");
            });
            t.Start();
        }
        var res2 = phorm.Call("GetConnectionCount");

        // Assert
        Assert.IsTrue((res2 - res1) < 10, $"First:{res1}, Last:{res2}");
    }
}