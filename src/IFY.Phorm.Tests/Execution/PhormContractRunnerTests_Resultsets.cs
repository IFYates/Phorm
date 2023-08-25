using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests_Resultsets
{
    class TestParent
    {
        public long Id { get; set; }
        public string? Key { get; set; }

        [Resultset(0, nameof(ChildrenSelector))]
        public TestChild[] Children { get; set; } = Array.Empty<TestChild>();
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
        [Resultset(0, nameof(TrueSelector))]
        public TestChild[] Children { get; } = Array.Empty<TestChild>();
        public static IRecordMatcher TrueSelector { get; }
            = new RecordMatcher<TestParentBadResultsetProperty, TestChild>((p, c) => true);
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
    public void Get__Entity_missing_default_constructor__Fail()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = Assert.ThrowsException<MissingMethodException>(() =>
        {
            _ = runner.Get<TestBadEntity>();
        });

        // Assert
        Assert.AreEqual("Attempt to get type IFY.Phorm.Execution.Tests.PhormContractRunnerTests_Resultsets+TestBadEntity without empty constructor.", ex.Message);
    }

    [TestMethod]
    public void GetAsync__Entity_missing_default_constructor__Fail()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = (MissingMethodException)Assert.ThrowsException<AggregateException>(() =>
        {
            _ = runner.GetAsync<TestBadEntity>(CancellationToken.None).Result;
        }).InnerException!;

        // Assert
        Assert.AreEqual("Attempt to get type IFY.Phorm.Execution.Tests.PhormContractRunnerTests_Resultsets+TestBadEntity without empty constructor.", ex.Message);
    }

    [TestMethod]
    public void ManyAsync__Resolves_secondary_resultset()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            },
            Results = new List<Dictionary<string, object>[]>(new[]
            {
                new[]
                {
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    }
                }
            })
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = runner.GetAsync<TestParent[]>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(1, res[0].Children.Length);
        Assert.AreEqual("value1", res[0].Children[0].Value);
    }

    [TestMethod]
    public void ManyAsync__Resultset_property_not_writable__Fail()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            },
            Results = new List<Dictionary<string, object>[]>(new[]
            {
                new[]
                {
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    }
                }
            })
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = (InvalidDataContractException)Assert.ThrowsException<AggregateException>(() =>
        {
            _ = runner.GetAsync<TestParentBadResultsetProperty[]>(CancellationToken.None).Result;
        }).InnerException!;

        // Assert
        Assert.AreEqual("Resultset property 'Children' is not writable.", ex.Message);
    }

    [TestMethod]
    public void One__Resolves_secondary_resultset()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            },
            Results = new List<Dictionary<string, object>[]>(new[]
            {
                new[]
                {
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    }
                }
            })
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = runner.GetAsync<TestParent>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(1, res?.Children.Length);
        Assert.AreEqual("value1", res?.Children[0].Value);
    }

    [TestMethod]
    public void One__Multiple_results_for_single_child__Fail()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            },
            Results = new List<Dictionary<string, object>[]>(new[]
            {
                Array.Empty<Dictionary<string, object>>(),
                new[]
                {
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    },
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value2"
                    }
                }
            })
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var ex = (InvalidCastException)Assert.ThrowsException<AggregateException>(() =>
        {
            _ = runner.GetAsync<TestParent>(CancellationToken.None).Result;
        }).InnerException!;
        Assert.IsTrue(ex.Message.Contains("not an array") == true);
    }

    [TestMethod]
    public void One__Resolves_single_child_entity()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Key"] = "key1"
                }
            },
            Results = new List<Dictionary<string, object>[]>(new[]
            {
                Array.Empty<Dictionary<string, object>>(),
                new[]
                {
                    new Dictionary<string, object>
                    {
                        ["ParentId"] = 1,
                        ["Value"] = "value1"
                    }
                }
            })
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);

        // Act
        var res = runner.GetAsync<TestParent>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual("value1", res?.Child?.Value);
    }
}