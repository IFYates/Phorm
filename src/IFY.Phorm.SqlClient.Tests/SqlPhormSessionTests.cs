﻿using IFY.Phorm.Connectivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;
using System.Reflection;

namespace IFY.Phorm.SqlClient.Tests
{
    [TestClass]
    public class SqlPhormSessionTests
    {
        [TestMethod]
        public void BeginTransaction()
        {
            // Arrange
            var mocks = new MockRepository(MockBehavior.Strict);

            var tranMock = mocks.Create<IDbTransaction>();

            var connMock = mocks.Create<IPhormDbConnection>();
            connMock.Setup(m => m.Open());
            connMock.Setup(m => m.BeginTransaction())
                .Returns(tranMock.Object).Verifiable();

            var connProvMock = mocks.Create<IPhormDbConnectionProvider>();
            connProvMock.Setup(m => m.GetConnection("name"))
                .Returns(connMock.Object).Verifiable();

            var sess = new SqlPhormSession(connProvMock.Object, "name");

            // Act
            var res = (TransactedSqlPhormSession)sess.BeginTransaction();

            // Assert
            mocks.Verify();
            Assert.AreSame(tranMock.Object, getField(res, "_transaction"));
            Assert.AreSame(connProvMock.Object, getField(res, "_connectionProvider"));
            Assert.AreEqual("name", getField<SqlPhormSession>(res, "_connectionName"));
        }

        [TestMethod]
        public void SetConnectionName__Returns_new_session_with_updated_connection_name()
        {
            // Arrange
            var connProvMock = new Mock<IPhormDbConnectionProvider>();

            var sess1 = new SqlPhormSession(connProvMock.Object, "name1");

            // Act
            var sess2 = (SqlPhormSession)sess1.SetConnectionName("name2");

            // Assert
            Assert.AreNotSame(sess2, sess1);
            Assert.AreEqual("name1", getField(sess1, "_connectionName"));
            Assert.AreEqual("name2", getField(sess2, "_connectionName"));
        }

        static object? getField<T>(T inst, string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(inst);
        }
    }
}