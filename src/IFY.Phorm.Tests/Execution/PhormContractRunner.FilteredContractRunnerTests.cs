using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace IFY.Phorm.Tests.Execution;

[TestClass]
public class PhormContractRunner
{
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
        public string? Value { get; set; }
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
        var ex = Assert.ThrowsException<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<Assert, IEnumerable<Assert>>(null!, null!));
        Assert.IsTrue(ex.Message.Contains("must have a public default constructor"), ex.Message);
    }

    [TestMethod]
    public void Constructor__Invalid_TResult_GenSpec__Fail()
    {
        var ex = Assert.ThrowsException<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<string, GenSpecBase<object>>(null!, null!));
        Assert.IsTrue(ex.Message.Contains("must use TResult as the GenSpec TBase"), ex.Message);
    }

    [TestMethod]
    public void Constructor__Invalid_TResult__Fail()
    {
        var ex = Assert.ThrowsException<ArgumentException>
            (() => new PhormContractRunner<IPhormContract>.FilteredContractRunner<string, object>(null!, null!));
        Assert.IsTrue(ex.Message.Contains("must match TResult or be GenSpec"), ex.Message);
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void GetAll__Can_filter_entities(bool byAsync)
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
                    ["Value"] = "value1"
                },
                new Dictionary<string, object>
                {
                    ["Value"] = "value2"
                },
                new Dictionary<string, object>
                {
                    ["Value"] = "value3"
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });
        var runner = parent.Where<TestDto>(o => o.Value != "value3");

        // Act
        var res = byAsync ? runner.GetAllAsync().Result : runner.GetAll();

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
    [DataRow(false), DataRow(true)]
    public void GetAll__Can_filter_GenSpec_entities(bool byAsync)
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
                    ["TypeId"] = 1, // BaseDto
                    ["Value"] = "valueB1",
                    ["Value2"] = "valueB2"
                },
                new Dictionary<string, object>
                {
                    ["TypeId"] = 2, // TestDto
                    ["Value"] = "valueC1",
                    ["Value2"] = "valueC2"
                },
                new Dictionary<string, object>
                {
                    ["TypeId"] = 1, // BaseDto
                    ["Value"] = "valueX" // Filtered out
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null);
        var runner = parent.Where<IDto, GenSpec<IDto, BaseDto, TestDto>>(o => o.Value != "valueX");

        // Act
        var res = byAsync ? runner.GetAllAsync().Result : runner.GetAll();

        // Assert
        var arr = res.All();
        Assert.AreEqual(2, arr.Length);
        Assert.IsInstanceOfType(arr[0], typeof(BaseDto));
        Assert.IsInstanceOfType(arr[1], typeof(TestDto));
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void GetAll__GenSpec__Unmatched_ignored_for_abstract_base(bool byAsync)
    {
        // Arrange
        var conn = new TestPhormConnection("");

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["TypeId"] = 0, // Unknown (ignored)
                    ["Value"] = "valueA",
                    ["Value2"] = "valueA2"
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null);
        var runner = parent.Where<IDto, GenSpec<IDto, BaseDto>>(o => o.Value != "valueX");

        // Act
        var res = byAsync ? runner.GetAllAsync().Result : runner.GetAll();

        // Assert
        Assert.AreEqual(0, res.Count());
    }

    [TestMethod]
    [DataRow(false), DataRow(true)]
    public void GetAll__GenSpec__Unmatched_as_concrete_base(bool byAsync)
    {
        // Arrange
        var conn = new TestPhormConnection("");

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["TypeId"] = 0, // Unmatched
                    ["Value"] = "valueA",
                    ["Value2"] = "valueA2"
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null);
        var runner = parent.Where<BaseDto, GenSpec<BaseDto, TestDto>>(o => o.Value != "valueX");

        // Act
        var res = byAsync ? runner.GetAllAsync().Result : runner.GetAll();

        // Assert
        var arr = res.All();
        Assert.IsInstanceOfType(arr.Single(), typeof(BaseDto));
    }

    [TestMethod]
    public void GetAll__Cancelled__Does_not_parse_results()
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
                    ["Value"] = "value1"
                },
                new Dictionary<string, object>
                {
                    ["Value"] = "value2"
                },
                new Dictionary<string, object>
                {
                    ["Value"] = "value3"
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var parent = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });
        var runner = parent.Where<TestDto>(o => o.Value != "value3");

        var token = new CancellationToken(true);

        // Act
        var res = runner.GetAllAsync(token).Result;

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
