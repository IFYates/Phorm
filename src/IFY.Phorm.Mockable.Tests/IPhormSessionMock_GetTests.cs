using IFY.Phorm.Data;
using Moq;

namespace IFY.Phorm.Mockable.Tests;

[TestClass]
public sealed class IPhormSessionMock_GetTests
{
    public TestContext TestContext { get; set; }

    [PhormContract(Name = "TestContract", Namespace = "Schema")]
    record TestModel(int Id);

    interface ITestContract : IPhormContract
    {
        int Id { get; }
    }

    // BeginTransaction
    [TestMethod]
    public async Task BeginTransaction_then_Get__Invokes_GetFrom_in_transaction()
    {

        // Arrange
        var mock = new Mock<IPhormSessionMock>(MockBehavior.Strict);

        var connectionName = Guid.NewGuid().ToString();
        var contextData = new Dictionary<string, object?>();

        IPhormSession phorm = mock.Object.ToMock();
        phorm = phorm.WithContext(connectionName, contextData);

        CallContext? callContext = null;
        var expectedResult = new TestModel(1);
        mock.Setup(m => m.GetFrom<ITestContract, TestModel>(
            It.Is<object>(a => a.IsLike<TestModel>(new { Id = 1 })),
            TestContext.CancellationToken,
            It.IsAny<CallContext>()))
            .Returns((object? args, CancellationToken token, CallContext context) =>
            {
                callContext = context;
                return expectedResult;
            });

        // Act
        var trans = await phorm.BeginTransactionAsync(default);
        var result = await trans.From<ITestContract>(new { Id = 1 })
            .GetAsync<TestModel>(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(expectedResult, result);
        Assert.IsNotNull(callContext);
        Assert.AreEqual(connectionName, callContext.ConnectionName);
        Assert.AreSame(contextData, callContext.ContextData);
        Assert.IsNull(callContext.TargetSchema);
        Assert.AreEqual("usp_TestContract", callContext.TargetObject);
        Assert.AreEqual(DbObjectType.StoredProcedure, callContext.TargetObjectType);
        Assert.IsTrue(callContext.IsInTransaction);
        Assert.AreEqual(((MockPhormSession)trans).TransactionId, callContext.TransactionId);
        Assert.IsFalse(callContext.IsReadOnly);
    }

    // CallAsync

    // From.Get
    [TestMethod]
    public async Task From_GetAsync_by_contract_name__Invokes_GetFrom()
    {
        // Arrange
        var mock = new Mock<IPhormSessionMock>(MockBehavior.Strict);

        var connectionName = Guid.NewGuid().ToString();
        var contextData = new Dictionary<string, object?>();
        var contractName = Guid.NewGuid().ToString();

        CallContext? callContext = null;
        var expectedResult = new TestModel(1);
        mock.Setup(m => m.GetFrom<TestModel>(
            contractName,
            It.Is<object>(a => a.IsLike<TestModel>(new { Id = 1 })),
            TestContext.CancellationToken,
            It.IsAny<CallContext>()))
            .Returns((string? cn, object? args, CancellationToken token, CallContext context) =>
            {
                callContext = context;
                return expectedResult;
            });

        IPhormSession phorm = mock.Object.ToMock();
        phorm = phorm.WithContext(connectionName, contextData);

        // Act
        var result = await phorm.From(contractName, new { Id = 1 })
            .GetAsync<TestModel>(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(expectedResult, result);
        Assert.IsNotNull(callContext);
        Assert.AreEqual(connectionName, callContext.ConnectionName);
        Assert.AreSame(contextData, callContext.ContextData);
        Assert.IsNull(callContext.TargetSchema);
        Assert.AreEqual("usp_" + contractName, callContext.TargetObject);
        Assert.AreEqual(DbObjectType.StoredProcedure, callContext.TargetObjectType);
        Assert.IsFalse(callContext.IsInTransaction);
        Assert.IsNull(callContext.TransactionId);
        Assert.IsFalse(callContext.IsReadOnly);
    }

    [TestMethod]
    public async Task From_GetAsync_by_contract__Invokes_GetFrom()
    {
        // Arrange
        var mock = new Mock<IPhormSessionMock>(MockBehavior.Strict);

        var connectionName = Guid.NewGuid().ToString();
        var contextData = new Dictionary<string, object?>();

        CallContext? callContext = null;
        var expectedResult = new TestModel(1);
        mock.Setup(m => m.GetFrom<ITestContract, TestModel>(
            It.Is<object>(a => a.IsLike<TestModel>(new { Id = 1 })),
            TestContext.CancellationToken,
            It.IsAny<CallContext>()))
            .Returns((object? args, CancellationToken token, CallContext context) =>
            {
                callContext = context;
                return expectedResult;
            });

        IPhormSession phorm = mock.Object.ToMock();
        phorm = phorm.WithContext(connectionName, contextData);

        // Act
        var result = await phorm.From<ITestContract>(new { Id = 1 })
            .GetAsync<TestModel>(TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(expectedResult, result);
        Assert.IsNotNull(callContext);
        Assert.AreEqual(connectionName, callContext.ConnectionName);
        Assert.AreSame(contextData, callContext.ContextData);
        Assert.IsNull(callContext.TargetSchema);
        Assert.AreEqual("usp_TestContract", callContext.TargetObject);
        Assert.AreEqual(DbObjectType.StoredProcedure, callContext.TargetObjectType);
        Assert.IsFalse(callContext.IsInTransaction);
        Assert.IsNull(callContext.TransactionId);
        Assert.IsFalse(callContext.IsReadOnly);
    }

    // From.Where.GetAll

    // Get
    [TestMethod]
    public async Task GetAsync_by_model__Invokes_GetFrom()
    {
        // Arrange
        var mock = new Mock<IPhormSessionMock>(MockBehavior.Strict);

        var connectionName = Guid.NewGuid().ToString();
        var contextData = new Dictionary<string, object?>();

        GlobalSettings.ViewPrefix = "View_";

        CallContext? callContext = null;
        var expectedResult = new TestModel(1);
        mock.Setup(m => m.GetFrom<TestModel>(
            null, // No contract name in call
            It.Is<object>(a => a.IsLike<TestModel>(new { Id = 1 })),
            TestContext.CancellationToken,
            It.IsAny<CallContext>()))
            .Returns((string? cn, object? args, CancellationToken token, CallContext context) =>
            {
                callContext = context;
                return expectedResult;
            });

        IPhormSession phorm = mock.Object.ToMock();
        phorm = phorm.WithContext(connectionName, contextData);

        // Act
        var result = await phorm.GetAsync<TestModel>(new { Id = 1 }, TestContext.CancellationToken);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreSame(expectedResult, result);
        Assert.IsNotNull(callContext);
        Assert.AreEqual(connectionName, callContext.ConnectionName);
        Assert.AreSame(contextData, callContext.ContextData);
        Assert.AreEqual("Schema", callContext.TargetSchema);
        Assert.AreEqual("View_TestContract", callContext.TargetObject);
        Assert.AreEqual(DbObjectType.View, callContext.TargetObjectType);
        Assert.IsFalse(callContext.IsInTransaction);
        Assert.IsNull(callContext.TransactionId);
        Assert.IsFalse(callContext.IsReadOnly);
    }
}
