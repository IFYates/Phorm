using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests_Resultsets
{
    public TestContext TestContext { get; set; }

    class TestParent
    {
        public long Id { get; set; }
        public string? Key { get; set; }

        [Resultset(0, nameof(ChildrenSelector))]
        public TestChild[] Children { get; set; } = [];
        public static IRecordMatcher ChildrenSelector { get; }
            = new RecordMatcher<TestParent, TestChild>((p, c) => c.ParentId == p.Id);

        [Resultset(1, nameof(ChildrenSelector))]
        public TestChild? Child { get; set; }
    }

    class TestChild
    {
        public long ParentId { get; set; }
        public string? Value { get; set; }
    }

    [ExcludeFromCodeCoverage]
    class TestParentBadResultsetProperty
    {
        [Resultset(0)]
        public TestChild[] Children { get; /*set;*/ } = [];
    }

    interface ITestContract : IPhormContract
    {
    }

    class TestBadEntity
    {
        [ExcludeFromCodeCoverage]
        public TestBadEntity(string value) { value.ToString(); }
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public async Task Get__Entity_missing_default_constructor__Fail()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<MissingMethodException>
            (async () => await runner.GetAsync<TestBadEntity>(TestContext.CancellationTokenSource.Token));

        // Assert
        Assert.AreEqual("Attempt to get type IFY.Phorm.Execution.Tests.PhormContractRunnerTests_Resultsets+TestBadEntity without empty constructor.", ex.Message);
    }

    [TestMethod]
    public async Task GetAsync__Entity_missing_default_constructor__Fail()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<MissingMethodException>
            (async () => await runner.GetAsync<TestBadEntity>(TestContext.CancellationTokenSource.Token));

        // Assert
        Assert.AreEqual("Attempt to get type IFY.Phorm.Execution.Tests.PhormContractRunnerTests_Resultsets+TestBadEntity without empty constructor.", ex.Message);
    }

    [TestMethod]
    public async Task ManyAsync__Resolves_secondary_resultset()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            ],
            Results =
            [
                [
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new()
                    {
                        ["Value"] = "value2"
                    }
                ]
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = await runner.GetAsync<TestParent[]>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(1, res![0].Children.Length);
        Assert.AreEqual("value1", res[0].Children[0].Value);
    }

    [TestMethod]
    public async Task ManyAsync__Resultset_property_not_writable__Fail()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            ],
            Results =
            [
                [
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    }
                ]
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidDataContractException>
            (async () => await runner.GetAsync<TestParentBadResultsetProperty[]>(TestContext.CancellationTokenSource.Token));

        // Assert
        Assert.AreEqual("Resultset property 'Children' is not writable.", ex.Message);
    }

    [TestMethod]
    public async Task One__Resolves_secondary_resultset()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            ],
            Results =
            [
                [
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new()
                    {
                        ["Value"] = "value2"
                    }
                ]
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = await runner.GetAsync<TestParent>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(1, res!.Children.Length);
        Assert.AreEqual("value1", res.Children[0].Value);
    }

    [TestMethod]
    public async Task One__Multiple_results_for_single_child__Fail()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            ],
            Results =
            [
                [],
                [
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value2"
                    }
                ]
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidCastException>
            (async () => await runner.GetAsync<TestParent>(TestContext.CancellationTokenSource.Token));

        // Assert
        Assert.Contains("not an array", ex.Message);
    }

    [TestMethod]
    public async Task One__Resolves_single_child_entity()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            ],
            Results =
            [
                [],
                [
                    new()
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    }
                ]
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = await runner.GetAsync<TestParent>(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual("value1", res!.Child?.Value);
    }
}