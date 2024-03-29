﻿using IFY.Phorm.Data;
using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class AbstractPhormSessionTests
{
    public interface ITestContract : IPhormContract
    {
    }
    [PhormContract(Name = "#Temp")]
    public interface ITempContract : IPhormContract
    {
    }

    class TestEntityView : ITestContract
    {
    }
    [PhormContract(Target = DbObjectType.Table)]
    class TestEntityTable : ITestContract
    {
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public void Defaults_prefixes_to_GlobalSettings()
    {
        // Act
        GlobalSettings.ProcedurePrefix = "PROC ";
        GlobalSettings.TablePrefix = "TABLE ";
        GlobalSettings.ViewPrefix = "VIEW ";

        var phorm2 = new TestPhormSession();

        GlobalSettings.ProcedurePrefix = "usp_";
        GlobalSettings.TablePrefix = string.Empty;
        GlobalSettings.ViewPrefix = "vw_";

        var phorm1 = new TestPhormSession();

        // Assert
        Assert.AreEqual("usp_", phorm1.ProcedurePrefix);
        Assert.AreEqual(string.Empty, phorm1.TablePrefix);
        Assert.AreEqual("vw_", phorm1.ViewPrefix);
        Assert.AreEqual("PROC ", phorm2.ProcedurePrefix);
        Assert.AreEqual("TABLE ", phorm2.TablePrefix);
        Assert.AreEqual("VIEW ", phorm2.ViewPrefix);
    }

    [TestMethod]
    public void ConnectionName__Returns_connection_name()
    {
        // Arrange
        var connName = Guid.NewGuid().ToString();

        var phorm = new TestPhormSession(connName);

        // Assert
        Assert.AreEqual(connName, phorm.ConnectionName);
    }

    [TestMethod]
    public void From__No_typearg()
    {
        // Arrange
        IPhormSession phorm = new TestPhormSession();

        // Act
        var runner = phorm.From("objectName");

        // Assert
        Assert.IsInstanceOfType(runner, typeof(PhormContractRunner<IPhormContract>));
    }

    [TestMethod]
    public void From__With_typearg()
    {
        // Arrange
        IPhormSession phorm = new TestPhormSession();

        // Act
        var runner = phorm.From<ITestContract>();

        // Assert
        Assert.IsInstanceOfType(runner, typeof(PhormContractRunner<ITestContract>));
    }

    [TestMethod]
    public void From__With_typed_arg()
    {
        // Arrange
        IPhormSession phorm = new TestPhormSession();

        var arg = new Mock<ITestContract>().Object;

        // Act
        var runner = phorm.From(arg);

        // Assert
        Assert.IsInstanceOfType(runner, typeof(PhormContractRunner<ITestContract>));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__By_name(bool byAsync)
    {
        // Arange
        var phorm = new TestPhormSession();

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync("TestContract").Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call("TestContract");
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__By_name_with_args(bool byAsync)
    {
        // Arange
        var phorm = new TestPhormSession();

        var args = new { Arg = 1 };

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync("TestContract", args).Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call("TestContract", args);
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        Assert.AreEqual("@Arg", ((TestDbDataParameter)phorm.Commands[0].Parameters[0]!).ParameterName);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__By_contract_object(bool byAsync)
    {
        // Arange
        var phorm = new TestPhormSession();

        var objMock = new Mock<ITestContract>();

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync(objMock.Object).Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call(objMock.Object);
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__Can_change_prefix(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession()
        {
            ProcedurePrefix = "PROC "
        };

        var objMock = new Mock<ITestContract>();

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync(objMock.Object).Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call(objMock.Object);
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[PROC TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__Temp_procedure__No_prefix(bool byAsync)
    {
        // Arange
        var phorm = new TestPhormSession();

        var objMock = new Mock<ITempContract>();

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync(objMock.Object).Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call(objMock.Object);
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[#Temp]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Call__By_typearg(bool byAsync)
    {
        // Arange
        var phorm = new TestPhormSession();

        // Act
        int res;
        if (byAsync)
        {
            res = ((IPhormSession)phorm).CallAsync<ITestContract>().Result;
        }
        else
        {
            res = ((IPhormSession)phorm).Call<ITestContract>();
        }

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
    }

    interface ITestContractArg : IPhormContract
    {
        int Arg { get; }
    }

    [TestMethod]
    public void CallAsync__By_typearg_with_anon_args()
    {
        // Arange
        var phorm = new TestPhormSession();

        var args = new { Arg = 1 };

        // Act
        var res = ((IPhormSession)phorm).CallAsync<ITestContractArg>(args).Result;

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContractArg]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        Assert.AreEqual("@Arg", ((TestDbDataParameter)phorm.Commands[0].Parameters[0]!).ParameterName);
    }

    [TestMethod]
    public void CallAsync__By_typearg__With_CT()
    {
        // Arange
        var phorm = new TestPhormSession();

        var token = new CancellationToken(true);

        // Act
        var res = ((IPhormSession)phorm).CallAsync<ITestContract>(token).Result;

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    public void CallAsync__By_contract_with_CT()
    {
        // Arange
        var phorm = new TestPhormSession();

        var objMock = new Mock<ITestContract>();

        var token = new CancellationToken(true);

        // Act
        var res = ((IPhormSession)phorm).CallAsync(objMock.Object, token).Result;

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    public void CallAsync__By_name__With_CT()
    {
        // Arange
        var phorm = new TestPhormSession();

        var token = new CancellationToken(true);

        // Act
        var res = ((IPhormSession)phorm).CallAsync("TestContract", token).Result;

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("[dbo].[usp_TestContract]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.StoredProcedure, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Get__By_typearg(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession();

        // Act
        ITestContract? result;
        if (byAsync)
        {
            result = ((IPhormSession)phorm).GetAsync<TestEntityView>().Result;
        }
        else
        {
            result = ((IPhormSession)phorm).Get<TestEntityView>();
        }

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[vw_TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    public void GetAsync__By_typearg_with_CT()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var token = new CancellationToken(true);

        // Act
        var result = ((IPhormSession)phorm).GetAsync<TestEntityView>(token).Result;

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[vw_TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Get__By_typed_arg(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var arg = new TestEntityView();

        // Act
        ITestContract? result;
        if (byAsync)
        {
            result = ((IPhormSession)phorm).GetAsync(arg).Result;
        }
        else
        {
            result = ((IPhormSession)phorm).Get(arg);
        }

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[vw_TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    public void Get__By_typed_arg_with_CT()
    {
        // Arrange
        var phorm = new TestPhormSession();

        var token = new CancellationToken(true);

        var arg = new TestEntityView();

        // Act
        var result = ((IPhormSession)phorm).GetAsync(arg, token).Result;

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[vw_TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Get__By_typearg_with_anon_args(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession();

        var arg = new { };

        // Act
        ITestContract? result;
        if (byAsync)
        {
            result = ((IPhormSession)phorm).GetAsync<TestEntityView>(arg).Result;
        }
        else
        {
            result = ((IPhormSession)phorm).Get<TestEntityView>(arg);
        }

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[vw_TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Get__View__Can_change_prefix(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession()
        {
            ViewPrefix = "VIEW "
        };

        var arg = new TestEntityView();

        // Act
        ITestContract? result;
        if (byAsync)
        {
            result = ((IPhormSession)phorm).GetAsync(arg).Result;
        }
        else
        {
            result = ((IPhormSession)phorm).Get(arg);
        }

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[VIEW TestEntityView]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Get__Table__Can_change_prefix(bool byAsync)
    {
        // Arrange
        var phorm = new TestPhormSession()
        {
            TablePrefix = "TABLE "
        };

        var arg = new TestEntityTable();

        // Act
        ITestContract? result;
        if (byAsync)
        {
            result = ((IPhormSession)phorm).GetAsync(arg).Result;
        }
        else
        {
            result = ((IPhormSession)phorm).Get(arg);
        }

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[TABLE TestEntityTable]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
    }

    [TestMethod]
    public void GetAsync__Table__By_contract_with_CT()
    {
        // Arrange
        var phorm = new TestPhormSession()
        {
            TablePrefix = "TABLE "
        };

        var arg = new TestEntityTable();

        var token = new CancellationToken(true);

        // Act
        var result = ((IPhormSession)phorm).GetAsync(arg, token).Result;

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual("SELECT * FROM [dbo].[TABLE TestEntityTable]", phorm.Commands[0].CommandText);
        Assert.AreEqual(CommandType.Text, phorm.Commands[0].CommandType);
        Assert.AreEqual(token, ((TestDbCommand)phorm.Commands[0]).ExecutionCancellationToken);
    }

    [TestMethod]
    public void CreateCommand__Unknown_DbObjectType__Fail()
    {
        var phorm = new TestPhormSession();
        Assert.ThrowsException<NotSupportedException>
            (() => phorm.CreateCommand("schema", "Object", (DbObjectType)255));
    }
}