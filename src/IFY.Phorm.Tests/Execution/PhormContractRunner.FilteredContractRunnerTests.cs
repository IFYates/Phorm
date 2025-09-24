using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using System.Data;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunner
{
    public TestContext TestContext { get; set; }

    interface IDto
    {
        string? Value { get; }
    }

    [PhormSpecOf(nameof(TypeId), 1)]
    class BaseDto : IDto
    {
        public int TypeId { get; set; }
        public string? Value { get; set; }
    }

    [PhormSpecOf(nameof(TypeId), 2)]
    class TestDto : BaseDto
    {
        public new string? Value { get; set; }
        public string? Value2 { get; set; }
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public void Constructor__TEntity_without_constructor__Fail()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<Assert, IEnumerable<Assert>>(null!, null!));
        Assert.Contains("must have a public default constructor", ex.Message, ex.Message);
    }

    [TestMethod]
    public void Constructor__Invalid_TResult_GenSpec__Fail()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<string, GenSpecBase<object>>(null!, null!));
        Assert.Contains("must use TResult as the GenSpec TBase", ex.Message, ex.Message);
    }

    [TestMethod]
    public void Constructor__Invalid_TResult__Fail()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<string, object>(null!, null!));
        Assert.Contains("must match TResult or be GenSpec", ex.Message, ex.Message);
    }

    [TestMethod]
    public async Task GetAll__Can_filter_entities()
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
                    ["Value"] = "value1"
                },
                new()
                {
                    ["Value"] = "value2"
                },
                new()
                {
                    ["Value"] = "value3"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 }, null);
        var runner = parent.Where<TestDto>(o => o.Value != "value3");

        // Act
        var res = await runner.GetAllAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(2, res.Count());
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);

        var arr = res.ToArray();
        Assert.AreEqual("value1", arr[0].Value);
        Assert.AreEqual("value2", arr[1].Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }

    [TestMethod]
    public async Task GetAll__Can_filter_GenSpec_entities()
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
                    ["TypeId"] = 1, // BaseDto
                    ["Value"] = "valueB1",
                    ["Value2"] = "valueB2"
                },
                new()
                {
                    ["TypeId"] = 2, // TestDto
                    ["Value"] = "valueC1",
                    ["Value2"] = "valueC2"
                },
                new()
                {
                    ["TypeId"] = 1, // BaseDto
                    ["Value"] = "valueX" // Filtered out
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);
        var runner = parent.Where<IDto, GenSpec<IDto, BaseDto, TestDto>>(o => o.Value != "valueX");

        // Act
        var res = await runner.GetAllAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        var arr = res.All();
        Assert.AreEqual(2, arr.Length);
        Assert.IsInstanceOfType(arr[0], typeof(BaseDto));
        Assert.IsInstanceOfType(arr[1], typeof(TestDto));
    }

    [TestMethod]
    public async Task GetAll__GenSpec__Unmatched_ignored_for_abstract_base()
    {
        // Arrange
        var conn = new TestPhormConnection("");

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["TypeId"] = 0, // Unknown (ignored)
                    ["Value"] = "valueA",
                    ["Value2"] = "valueA2"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);
        var runner = parent.Where<IDto, GenSpec<IDto, BaseDto>>(o => o.Value != "valueX");

        // Act
        var res = await runner.GetAllAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        Assert.AreEqual(0, res.Count());
    }

    [TestMethod]
    public async Task GetAll__GenSpec__Unmatched_as_concrete_base()
    {
        // Arrange
        var conn = new TestPhormConnection("");

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["TypeId"] = 0, // Unmatched
                    ["Value"] = "valueA",
                    ["Value2"] = "valueA2"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);
        var runner = parent.Where<BaseDto, GenSpec<BaseDto, TestDto>>(o => o.Value != "valueX");

        // Act
        var res = await runner.GetAllAsync(TestContext.CancellationTokenSource.Token);

        // Assert
        var arr = res.All();
        Assert.IsInstanceOfType(arr.Single(), typeof(BaseDto));
    }

    [TestMethod]
    public async Task GetAll__Cancelled__Does_not_parse_results()
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
                    ["Value"] = "value1"
                },
                new()
                {
                    ["Value"] = "value2"
                },
                new()
                {
                    ["Value"] = "value3"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 }, null);
        var runner = parent.Where<TestDto>(o => o.Value != "value3");

        var token = new CancellationToken(true);

        // Act
        var res = await runner.GetAllAsync(token);

        // Assert
        Assert.AreEqual(0, res.Count());
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }
}
