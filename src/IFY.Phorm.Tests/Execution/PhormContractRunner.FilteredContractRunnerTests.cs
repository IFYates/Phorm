using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace IFY.Phorm.Tests.Execution;

[TestClass]
public class PhormContractRunner
{
    class TestDto
    {
        public string? Value { get; set; }
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
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
