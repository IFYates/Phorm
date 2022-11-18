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