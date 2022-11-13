using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace IFY.Phorm.Tests.Encryption;

[TestClass]
public class TransformationTests
{
    class DataObject
    {
        [TransformFromSource]
        public string? Value { get; set; }
    }

    interface IWithTransformation : IPhormContract
    {
        [TransformToSource]
        string? Value { get; }
    }

    class TransformToSourceAttribute : AbstractTransphormAttribute
    {
        [ExcludeFromCodeCoverage]
        public override object? FromDatasource(Type type, object? data, object? context)
            => throw new NotImplementedException();
        public override object? ToDatasource(object? data, object? context)
            => "ToSource_" + (string?)data;
    }

    class TransformFromSourceAttribute : AbstractTransphormAttribute
    {
        public override object? FromDatasource(Type type, object? data, object? context)
            => "FromSource_" + (string?)data;
        [ExcludeFromCodeCoverage]
        public override object? ToDatasource(object? data, object? context)
            => throw new NotImplementedException();
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public void Can_transform_value_to_datasource()
    {
        // Arrange
        var runner = new TestPhormSession();

        var args = new { Value = "value" };

        // Act
        var res = runner.Call<IWithTransformation>(args);

        // Assert
        Assert.AreEqual(1, res);

        var testCmd = runner.Commands.Single();
        Assert.AreEqual("ToSource_value", ((IDataParameter)testCmd.Parameters["@Value"]).Value);
    }

    [TestMethod]
    public void Can_transform_value_from_datasource()
    {
        // Arrange
        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["Value"] = "value"
                }
            }
        });

        var runner = new TestPhormSession();
        runner.TestConnection?.CommandQueue.Enqueue(cmd);

        // Act
        var res = runner.From("Get").Get<DataObject>();

        // Assert
        Assert.IsNotNull(res);
        Assert.AreEqual("FromSource_value", res?.Value);
    }
}
