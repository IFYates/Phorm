using IFY.Phorm.Data;
using IFY.Phorm.Execution;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ResultsetTests : SqlIntegrationTestBase
{
    public class ManyParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        [Resultset(0, nameof(ChildrenMatcher))]
        public ChildDTO[] Children { get; set; } = null!;
        public static RecordMatcher<ManyParentDTO, ChildDTO> ChildrenMatcher => new((p, c) => c.ParentId == p.Id);
    }
    public class OneParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        [Resultset(0, nameof(ChildrenMatcher))]
        public ChildDTO? Child { get; set; }
        public static RecordMatcher<OneParentDTO, ChildDTO> ChildrenMatcher => new((p, c) => c.ParentId == p.Id);
    }
    public class EveryParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;

        [Resultset(0)]
        public ChildDTO[] Children { get; set; } = null!;
    }
    public class ChildDTO
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public string Name { get; set; } = null!;
    }

    public interface IGetFamily : IPhormContract
    {
    }

    private async Task setupGetTestSchema(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, [
            @"DROP PROCEDURE IF EXISTS [dbo].[usp_GetFamily]",
            @"DROP TABLE IF EXISTS [dbo].[Child]",
            @"DROP TABLE IF EXISTS [dbo].[Parent]",
            @"CREATE TABLE [dbo].[Parent] (
	[Id] BIGINT NOT NULL PRIMARY KEY,
    [Name] VARCHAR(50) NOT NULL UNIQUE
)",
            @"CREATE TABLE [dbo].[Child] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [ParentId] BIGINT NOT NULL REFERENCES [dbo].[Parent]([Id]),
    [Name] VARCHAR(50) NOT NULL UNIQUE
)",
            @"CREATE PROCEDURE [dbo].[usp_GetFamily] AS
	SELECT * FROM [dbo].[Parent]
	SELECT * FROM [dbo].[Child]
RETURN 1"
        ]);
    }

    [TestMethod]
    public async Task Parents_match_multiple_children_from_resultset()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, [
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'",
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 2, 'Two.One'"
        ]);

        // Act
        var res = await phorm.From<IGetFamily>(null)
            .GetAsync<ManyParentDTO[]>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(2, res!.Length);
        Assert.AreEqual("One", res[0].Name);
        Assert.AreEqual(2, res[0].Children.Length);
        Assert.AreEqual("One.One", res[0].Children[0].Name);
        Assert.AreEqual("One.Two", res[0].Children[1].Name);
        Assert.AreEqual("Two", res[1].Name);
        Assert.AreEqual(1, res[1].Children.Length);
        Assert.AreEqual("Two.One", res[1].Children[0].Name);
    }

    [TestMethod]
    public async Task Parents_expect_none_or_one_child()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, [
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'",
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'"
        ]);

        // Act
        var res = await phorm.From<IGetFamily>(null)
            .GetAsync<OneParentDTO[]>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(2, res!.Length);
        Assert.AreEqual("One", res[0].Name);
        Assert.AreEqual("One.One", res[0].Child!.Name);
        Assert.AreEqual("Two", res[1].Name);
        Assert.IsNull(res[1].Child);
    }

    [TestMethod]
    public async Task Parents_expect_none_or_one_child__Receives_many__Exception()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, [
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'"
        ]);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidCastException>
            (async () => await phorm.From<IGetFamily>(null).GetAsync<OneParentDTO[]>(TestContext.CancellationTokenSource.Token));

        // Assert
        Assert.AreEqual("Phorm Resultset property Child is not an array but matched 2 records.", ex.Message);
    }

    [TestMethod]
    public async Task Parents_can_match_every_child_from_resultset()
    {
        // Arrange
        var phorm = getPhormSession();
        await setupGetTestSchema(phorm);

        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationTokenSource.Token, [
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'",
            "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'",
            "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 2, 'Two.One'"
        ]);

        // Act
        var res = await phorm.From<IGetFamily>(null)
            .GetAsync<EveryParentDTO[]>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(2, res!.Length);
        Assert.AreEqual("One", res[0].Name);
        Assert.AreEqual(3, res[0].Children.Length);
        Assert.AreEqual("One.One", res[0].Children[0].Name);
        Assert.AreEqual("One.Two", res[0].Children[1].Name);
        Assert.AreEqual("Two.One", res[0].Children[2].Name);
        Assert.AreEqual("Two", res[1].Name);
        Assert.AreEqual(3, res[1].Children.Length);
        Assert.AreEqual("One.One", res[1].Children[0].Name);
        Assert.AreEqual("One.Two", res[1].Children[1].Name);
        Assert.AreEqual("Two.One", res[1].Children[2].Name);
    }
}