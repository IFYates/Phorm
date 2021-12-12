using IFY.Phorm.Data;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace IFY.Phorm.Tests.Encryption
{
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
            public override object? FromDatasource(Type type, object? data) => throw new NotImplementedException();
            public override object? ToDatasource(object? data) => "ToSource_" + (string?)data;
        }

        class TransformFromSourceAttribute : AbstractTransphormAttribute
        {
            public override object? FromDatasource(Type type, object? data) => "FromSource_" + (string?)data;
            public override object? ToDatasource(object? data) => throw new NotImplementedException();
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
            var cmd = new TestDbCommand(new TestDbReader
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
            runner.TestConnectionProvider?.TestConnection?.CommandQueue.Enqueue(cmd);

            // Act
            var res = runner.From("Get").One<DataObject>();

            // Assert
            Assert.IsNotNull(res);
            Assert.AreEqual("FromSource_value", res?.Value);
        }
    }
}
