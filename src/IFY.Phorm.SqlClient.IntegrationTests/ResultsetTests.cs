using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ResultsetTests : SqlIntegrationTestBase
{
    public class ManyParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }

        [Resultset(0, nameof(ChildrenMatcher))]
        public ChildDTO[] Children { get; set; }
        public static RecordMatcher<ManyParentDTO, ChildDTO> ChildrenMatcher => new((p, c) => c.ParentId == p.Id);
    }
    public class OneParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }

        [Resultset(0, nameof(ChildrenMatcher))]
        public ChildDTO? Child { get; set; }
        public static RecordMatcher<OneParentDTO, ChildDTO> ChildrenMatcher => new((p, c) => c.ParentId == p.Id);
    }
    public class EveryParentDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }

        [Resultset(0)]
        public ChildDTO[] Children { get; set; }
    }
    public class ChildDTO
    {
        public long Id { get; set; }
        public long ParentId { get; set; }
        public string Name { get; set; }
    }

    public interface IGetFamily : IPhormContract
    {
    }

    private void setupGetTestSchema(AbstractPhormSession phorm)
    {
        SqlTestHelpers.ApplySql(phorm, @"DROP PROCEDURE IF EXISTS [dbo].[usp_GetFamily]");
        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[Child]");
        SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[Parent]");

        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[Parent] (
	[Id] BIGINT NOT NULL PRIMARY KEY,
    [Name] VARCHAR(50) NOT NULL UNIQUE
)");

        SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[Child] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
    [ParentId] BIGINT NOT NULL REFERENCES [dbo].[Parent]([Id]),
    [Name] VARCHAR(50) NOT NULL UNIQUE
)");

        SqlTestHelpers.ApplySql(phorm, @"CREATE PROCEDURE [dbo].[usp_GetFamily] AS
	SELECT * FROM [dbo].[Parent]
	SELECT * FROM [dbo].[Child]
RETURN 1");
    }

    [TestMethod]
    public void Parents_match_multiple_children_from_resultset()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'");
        
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 2, 'Two.One'");

        // Act
        var res = phorm.From<IGetFamily>(null)
            .Get<ManyParentDTO[]>()!;

        // Assert
        Assert.AreEqual(2, res.Length);
        Assert.AreEqual("One", res[0].Name);
        Assert.AreEqual(2, res[0].Children.Length);
        Assert.AreEqual("One.One", res[0].Children[0].Name);
        Assert.AreEqual("One.Two", res[0].Children[1].Name);
        Assert.AreEqual("Two", res[1].Name);
        Assert.AreEqual(1, res[1].Children.Length);
        Assert.AreEqual("Two.One", res[1].Children[0].Name);
    }

    [TestMethod]
    public void Parents_expect_none_or_one_child()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'");

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'");

        // Act
        var res = phorm.From<IGetFamily>(null)
            .Get<OneParentDTO[]>()!;

        // Assert
        Assert.AreEqual(2, res.Length);
        Assert.AreEqual("One", res[0].Name);
        Assert.AreEqual("One.One", res[0].Child!.Name);
        Assert.AreEqual("Two", res[1].Name);
        Assert.IsNull(res[1].Child);
    }

    [TestMethod]
    public void Parents_expect_none_or_one_child__Receives_many__Exception()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'");

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'");

        // Act
        var ex = Assert.ThrowsException<InvalidCastException>
            (() => phorm.From<IGetFamily>(null).Get<OneParentDTO[]>()!);

        // Assert
        Assert.AreEqual("Resultset property Child is not an array but matched 2 records.", ex.Message);
    }

    [TestMethod]
    public void Parents_can_match_every_child_from_resultset()
    {
        // Arrange
        var phorm = getPhormSession();
        setupGetTestSchema(phorm);

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 1, 'One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Parent] ([Id], [Name]) SELECT 2, 'Two'");

        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.One'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 1, 'One.Two'");
        SqlTestHelpers.ApplySql(phorm, "INSERT INTO [dbo].[Child] ([ParentId], [Name]) SELECT 2, 'Two.One'");

        // Act
        var res = phorm.From<IGetFamily>(null)
            .Get<EveryParentDTO[]>()!;

        // Assert
        Assert.AreEqual(2, res.Length);
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