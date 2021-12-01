using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests
    {
        public interface IContractDTO : IPhormContract
        {
        }
        [PhormContract(Namespace = "schema", Name = "contractName", Target = DbObjectType.Table)]
        public interface IContractWithAttributeDTO : IPhormContract
        {
        }
        [PhormContract(Namespace = "schema", Name = "contractName")]
        public interface IContractWithAttributeNoTargetDTO : IPhormContract
        {
        }
        [DataContract(Namespace = "schema", Name = "contractName")]
        public class DataContractDTO : IPhormContract
        {
        }

        private static T? getFieldValue<T>(object obj, string fieldName)
        {
            return (T?)obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
        }

        [TestMethod]
        public void PhormContractRunner__Anonymous_Gets_contract_info()
        {
            // Act
            var runner = new PhormContractRunner<IPhormContract>(null, "objectName", DbObjectType.Table);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("objectName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Anonymous_No_name__Exception()
        {
            // Act
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                _ = new PhormContractRunner<IPhormContract>(null, null, DbObjectType.StoredProcedure);
            });
        }

        [TestMethod]
        public void PhormContractRunner__Anonymous_Default_objectType_is_StoredProcedure()
        {
            // Act
            var runner = new PhormContractRunner<IPhormContract>(null, "objectName", DbObjectType.Default);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("objectName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract__Takes_value()
        {
            // Act
            var runner = new PhormContractRunner<IContractDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract__Ignores_name_override()
        {
            // Act
            var runner = new PhormContractRunner<IContractDTO>(null, "objectName", DbObjectType.Table);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract_with_attribute__Takes_values()
        {
            // Act
            var runner = new PhormContractRunner<IContractWithAttributeDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract_with_attribute__Ignores_overrides()
        {
            // Act
            var runner = new PhormContractRunner<IContractWithAttributeDTO>(null, "objectName", DbObjectType.View);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__DataContract__Takes_values()
        {
            // Act
            var runner = new PhormContractRunner<DataContractDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__DataContract__Ignores_name_override()
        {
            // Act
            var runner = new PhormContractRunner<DataContractDTO>(null, "objectName", DbObjectType.View);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.View, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void CallTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void CallAsyncTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void CallTest1()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void CallAsyncTest1()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void ManyTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void ManyAsyncTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void ManyTest1()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void ManyAsyncTest1()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void OneTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void OneAsyncTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void OneTest1()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void OneAsyncTest1()
        {
            Assert.Inconclusive();
        }
    }
}