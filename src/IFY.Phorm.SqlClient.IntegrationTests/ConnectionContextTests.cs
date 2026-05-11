using IFY.Phorm.Execution;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ConnectionContextTests : SqlIntegrationTestBase
{
    public record Data(string Key, string? Value);

    private async Task setContextTestContract(AbstractPhormSession connProv)
    {
        await SqlTestHelpers.ApplySql(connProv, TestContext.CancellationToken, @"CREATE OR ALTER PROC [dbo].[usp_ContextTest]
AS
	SET NOCOUNT ON
    SELECT N'Key1' [Key], SESSION_CONTEXT(N'Key1') [Value]
    UNION ALL
    SELECT N'Key2', SESSION_CONTEXT(N'Key2') [Value]
    UNION ALL
    SELECT N'Key3', SESSION_CONTEXT(N'Key3') [Value]
RETURN 1");
    }

    [TestMethod]
    public async Task Session_ContextData_is_accessible_in_database()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");
        await setContextTestContract(phorm);

        var value = Guid.NewGuid().ToString();

        // Act
        var sess = phorm.WithContext(null, new Dictionary<string, object?>
        {
            { "Key1", value }, // Set
            { "Key2", DBNull.Value }, // Explicitly unset
            // Key3 implicitly unset
        });

        var res = await sess.From("ContextTest", null)
            .GetAsync<Data[]>(TestContext.CancellationToken);

        // Assert
        Assert.HasCount(3, res!);
        Assert.AreEqual(value, res!.Single(d => d.Key == "Key1").Value);
        Assert.IsNull(res!.Single(d => d.Key == "Key2").Value);
        Assert.IsNull(res!.Single(d => d.Key == "Key3").Value);
    }

    [TestMethod]
    public async Task Session_ContextData_persists_for_session()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");
        await setContextTestContract(phorm);

        var value = Guid.NewGuid().ToString();

        // Act
        var sess = phorm.WithContext(null, new Dictionary<string, object?>
        {
            { "Key1", value }, // Set
            { "Key2", DBNull.Value }, // Explicitly unset
            // Key3 implicitly unset
        });

        _ = await sess.From("ContextTest", null)
            .GetAsync<Data[]>(TestContext.CancellationToken);
        var res = await sess.From("ContextTest", null)
            .GetAsync<Data[]>(TestContext.CancellationToken);

        // Assert
        Assert.HasCount(3, res!);
        Assert.AreEqual(value, res!.Single(d => d.Key == "Key1").Value);
        Assert.IsNull(res!.Single(d => d.Key == "Key2").Value);
        Assert.IsNull(res!.Single(d => d.Key == "Key3").Value);
    }

    [TestMethod]
    public async Task Session_ContextData_does_not_affect_other_sessions()
    {
        // Arrange
        var phorm = getPhormSession("TestContext");
        await setContextTestContract(phorm);

        var value = Guid.NewGuid().ToString();

        // Act
        var sess = phorm.WithContext(null, new Dictionary<string, object?>
        {
            { "Key1", value }, // Set
            { "Key2", DBNull.Value }, // Explicitly unset
            // Key3 implicitly unset
        });

        var res = await phorm.From("ContextTest", null) // Not the contexted session
            .GetAsync<Data[]>(TestContext.CancellationToken);

        // Assert
        Assert.IsEmpty(res!.Where(r => r.Value != null));
    }
}