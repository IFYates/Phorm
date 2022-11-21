using IFY.Phorm.Data;
using IFY.Phorm.ExampleApp.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.Mockable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IFY.Phorm.ExampleApp.Tests;

[TestClass]
public class DbGatewayTests
{
    [TestMethod]
    public void CreateEmployee__Without_Mockable_framework()
    {
        // Arrange
        var arg = new EmployeeDto();

        var phormSessionMock = new Mock<IPhormSession>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.Call<ICreateEmployee>(arg))
            .Returns(1);

        var dbGateway = new DbGateway(phormSessionMock.Object);

        // Act
        var res = dbGateway.CreateEmployee(arg);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void CreateEmployee__Using_Mockable_framework()
    {
        // Arrange
        var arg = new EmployeeDto();

        var phormSessionMock = new Mock<IPhormSessionMock>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.Call<ICreateEmployee>(arg, It.IsAny<CallContext>()))
            .Returns(1);

        var dbGateway = new DbGateway(phormSessionMock.Object.ToMock());

        // Act
        var res = dbGateway.CreateEmployee(arg);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void CreateManager__Without_Mockable_framework()
    {
        // Arrange
        var arg = new ManagerDto();

        var phormSessionMock = new Mock<IPhormSession>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.Call<ICreateManager>(arg))
            .Returns(1);

        var dbGateway = new DbGateway(phormSessionMock.Object);

        // Act
        var res = dbGateway.CreateManager(arg);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void CreateManager__Using_Mockable_framework()
    {
        // Arrange
        var arg = new ManagerDto();

        var phormSessionMock = new Mock<IPhormSessionMock>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.Call<ICreateManager>(arg, It.IsAny<CallContext>()))
            .Returns(1);

        var dbGateway = new DbGateway(phormSessionMock.Object.ToMock());

        // Act
        var res = dbGateway.CreateManager(arg);

        // Assert
        Assert.IsTrue(res);
    }

    [TestMethod]
    public void GetAllPeople__Without_Mockable_framework()
    {
        // Arrange
        var data = new GenSpec<PersonDto, EmployeeDto, ManagerDto>();

        var contractRunnerMock = new Mock<IPhormContractRunner<IGetAllPeople>>(MockBehavior.Strict);
        contractRunnerMock.Setup(m => m.Get<GenSpec<PersonDto, EmployeeDto, ManagerDto>>())
            .Returns(data);

        var phormSessionMock = new Mock<IPhormSession>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.From<IGetAllPeople>())
            .Returns(contractRunnerMock.Object);

        var dbGateway = new DbGateway(phormSessionMock.Object);

        // Act
        var res = dbGateway.GetAllPeople();

        // Assert
        Assert.AreSame(data, res);
    }

    [TestMethod]
    public void GetAllPeople__Using_Mockable_framework()
    {
        // Arrange
        var data = new GenSpec<PersonDto, EmployeeDto, ManagerDto>();

        var phormSessionMock = new Mock<IPhormSessionMock>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.GetFrom<IGetAllPeople, GenSpec<PersonDto, EmployeeDto, ManagerDto>>(null, It.IsAny<CallContext>()))
            .Returns(data);

        var dbGateway = new DbGateway(new MockPhormSession(phormSessionMock.Object));

        // Act
        var res = dbGateway.GetAllPeople();

        // Assert
        Assert.AreSame(data, res);
    }

    [TestMethod]
    public void GetManager__Without_Mockable_framework()
    {
        // Arrange
        var data = new ManagerDtoWithEmployees();

        var contractRunnerMock = new Mock<IPhormContractRunner<IGetManager>>(MockBehavior.Strict);
        contractRunnerMock.Setup(m => m.Get<ManagerDtoWithEmployees>())
            .Returns(data);

        long? fromArg_Id = null;
        var phormSessionMock = new Mock<IPhormSession>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.From<IGetManager>(It.IsAny<object>()))
            .Returns<object>(o =>
            {
                fromArg_Id = (long)o.GetType().GetProperty("Id")!.GetValue(o)!;
                return contractRunnerMock.Object;
            });

        var dbGateway = new DbGateway(phormSessionMock.Object);

        long id = new Random().Next();

        // Act
        var res = dbGateway.GetManager(id);

        // Assert
        Assert.AreEqual(id, fromArg_Id);
        Assert.AreSame(data, res);
    }

    [TestMethod]
    public void GetManager__Using_Mockable_framework()
    {
        // Arrange
        var data = new ManagerDtoWithEmployees();

        long? fromArg_Id = null;
        var phormSessionMock = new Mock<IPhormSessionMock>(MockBehavior.Strict);
        phormSessionMock.Setup(m => m.GetFrom<IGetManager, ManagerDtoWithEmployees>(It.IsAny<object>(), It.IsAny<CallContext>()))
            .Returns<object, CallContext>((o, c) =>
            {
                fromArg_Id = (long)o.GetType().GetProperty("Id")!.GetValue(o)!;
                return data;
            });

        var dbGateway = new DbGateway(new MockPhormSession(phormSessionMock.Object));

        long id = new Random().Next();

        // Act
        var res = dbGateway.GetManager(id);

        // Assert
        Assert.AreEqual(id, fromArg_Id);
        Assert.AreSame(data, res);
    }
}