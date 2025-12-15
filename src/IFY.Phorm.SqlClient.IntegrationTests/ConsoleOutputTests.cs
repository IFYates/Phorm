using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.SqlClient.IntegrationTests.Helpers;
using Microsoft.Data.SqlClient;

namespace IFY.Phorm.SqlClient.IntegrationTests;

[TestClass]
public class ConsoleOutputTests : SqlIntegrationTestBase
{
    public interface IConsoleTest : IPhormContract
    {
        string? Text { get; }
    }
    public class ConsoleTest : IConsoleTest
    {
        public string? Text { get; set; }

        public ConsoleLogMember ConsoleEvents { get; set; } = ContractMember.Console();
    }

    [TestInitialize]
    public void Init()
    {
        enableGlobalEventHandlers();
    }
    [TestCleanup]
    public void Cleanup()
    {
        disableGlobalEventHandlers();
    }

    private async Task setConsoleOutputContract(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, @"CREATE OR ALTER PROC [dbo].[usp_ConsoleTest]
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	RAISERROR ('Before', 0, 1) WITH NOWAIT;
	RAISERROR (@Text, 2, 3) WITH NOWAIT;
	RAISERROR ('After', 4, 5) WITH NOWAIT;
    PRINT 'End'
RETURN 1");
    }
    private async Task setConsoleOutputErrorContract(AbstractPhormSession phorm)
    {
        await SqlTestHelpers.ApplySql(phorm, TestContext.CancellationToken, @"CREATE OR ALTER PROC [dbo].[usp_ConsoleTest]
AS
	SET NOCOUNT ON
	RAISERROR ('Before', 1, 1) WITH NOWAIT;
	SELECT 1 / 0
	RAISERROR ('After', 1, 1) WITH NOWAIT;
    PRINT 'End'
RETURN 1");
    }

    [TestMethod]
    public async Task Console_output__Contract_member__Get_all_events_in_order()
    {
        // Arrange
        var phorm = getPhormSession();
        await setConsoleOutputContract(phorm);

        var arg = new ConsoleTest
        {
            Text = DateTime.UtcNow.ToString("o")
        };

        // Act
        var res = await phorm.CallAsync<IConsoleTest>(arg, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        var events = arg.ConsoleEvents.Value.ToArray();
        Assert.HasCount(4, events);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 5", events[0].Source);
        Assert.AreEqual("Before", events[0].Message);
        Assert.AreEqual(0, events[0].Level);
        Assert.IsFalse(events[0].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 6", events[1].Source);
        Assert.AreEqual(arg.Text, events[1].Message);
        Assert.AreEqual(2, events[1].Level);
        Assert.IsFalse(events[1].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 7", events[2].Source);
        Assert.AreEqual("After", events[2].Message);
        Assert.AreEqual(4, events[2].Level);
        Assert.IsFalse(events[2].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 8", events[3].Source);
        Assert.AreEqual("End", events[3].Message);
        Assert.AreEqual(0, events[3].Level);
        Assert.IsFalse(events[3].IsError);
    }

    [TestMethod]
    public async Task Console_output__Anonymous_contract_member__Get_all_events_in_order()
    {
        // Arrange
        var phorm = getPhormSession();
        await setConsoleOutputContract(phorm);

        var arg = new
        {
            Text = DateTime.UtcNow.ToString("o"),
            ConsoleEvents = ContractMember.Console()
        };

        // Act
        var res = await phorm.CallAsync<IConsoleTest>(arg, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        var events = arg.ConsoleEvents.Value.ToArray();
        Assert.HasCount(4, events);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 5", events[0].Source);
        Assert.AreEqual("Before", events[0].Message);
        Assert.AreEqual(0, events[0].Level);
        Assert.IsFalse(events[0].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 6", events[1].Source);
        Assert.AreEqual(arg.Text, events[1].Message);
        Assert.AreEqual(2, events[1].Level);
        Assert.IsFalse(events[1].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 7", events[2].Source);
        Assert.AreEqual("After", events[2].Message);
        Assert.AreEqual(4, events[2].Level);
        Assert.IsFalse(events[2].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 8", events[3].Source);
        Assert.AreEqual("End", events[3].Message);
        Assert.AreEqual(0, events[3].Level);
        Assert.IsFalse(events[3].IsError);
    }

    [TestMethod]
    [DataRow(false, DisplayName = "Instance")]
    [DataRow(true, DisplayName = "Global")]
    public async Task Console_output__Event__Get_all_events_in_order(bool asGlobal)
    {
        // Arrange
        var phorm = getPhormSession();
        await setConsoleOutputContract(phorm);

        var events = new List<ConsoleMessage>();
        if (asGlobal)
        {
            _globalConsoleMessage += (_, a) => events.Add(a.ConsoleMessage);
        }
        else
        {
            phorm.ConsoleMessage += (_, a) => events.Add(a.ConsoleMessage);
        }

        var arg = new
        {
            Text = DateTime.UtcNow.ToString("o")
        };

        // Act
        var res = await phorm.CallAsync<IConsoleTest>(arg, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.HasCount(4, events);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 5", events[0].Source);
        Assert.AreEqual("Before", events[0].Message);
        Assert.AreEqual(0, events[0].Level);
        Assert.IsFalse(events[0].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 6", events[1].Source);
        Assert.AreEqual(arg.Text, events[1].Message);
        Assert.AreEqual(2, events[1].Level);
        Assert.IsFalse(events[1].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 7", events[2].Source);
        Assert.AreEqual("After", events[2].Message);
        Assert.AreEqual(4, events[2].Level);
        Assert.IsFalse(events[2].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 8", events[3].Source);
        Assert.AreEqual("End", events[3].Message);
        Assert.AreEqual(0, events[3].Level);
        Assert.IsFalse(events[3].IsError);
    }

    [TestMethod]
    public async Task Error__Call__Can_receive_error_info_as_message()
    {
        // Arrange
        var phorm = getPhormSession();
        phorm.ExceptionsAsConsoleMessage = true;
        await setConsoleOutputErrorContract(phorm);

        var arg = new ConsoleTest();

        // Act
        var res = await phorm.CallAsync<IConsoleTest>(arg, TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(0, res);
        var events = arg.ConsoleEvents.Value;
        Assert.HasCount(2, events);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 4", events[0].Source);
        Assert.AreEqual("Before", events[0].Message);
        Assert.AreEqual(1, events[0].Level);
        Assert.IsFalse(events[0].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 5", events[1].Source);
        Assert.AreEqual("Divide by zero error encountered.", events[1].Message);
        Assert.AreEqual(16, events[1].Level);
        Assert.IsTrue(events[1].IsError);
    }

    [TestMethod]
    public async Task Error__Call_without_capture__Will_fail_execution()
    {
        // Arrange
        var phorm = getPhormSession();
        phorm.ExceptionsAsConsoleMessage = false;
        await setConsoleOutputErrorContract(phorm);

        var arg = new ConsoleTest();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<SqlException>
            (async () => await phorm.CallAsync<IConsoleTest>(arg, TestContext.CancellationToken));

        // Assert
        Assert.AreEqual("Divide by zero error encountered.", ex.Message);
    }

    [TestMethod]
    public async Task Error__Get__Can_receive_error_info_as_message()
    {
        // Arrange
        var phorm = getPhormSession();
        phorm.ExceptionsAsConsoleMessage = true;
        await setConsoleOutputErrorContract(phorm);

        var arg = new
        {
            ConsoleEvents = ContractMember.Console(),
            ReturnValue = ContractMember.RetVal()
        };

        // Act
        await phorm.From<IConsoleTest>(arg).GetAsync<object>(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(0, arg.ReturnValue.Value);
        var events = arg.ConsoleEvents.Value;
        Assert.HasCount(2, events);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 4", events[0].Source);
        Assert.AreEqual("Before", events[0].Message);
        Assert.AreEqual(1, events[0].Level);
        Assert.IsFalse(events[0].IsError);
        Assert.AreEqual("dbo.usp_ConsoleTest @ 5", events[1].Source);
        Assert.AreEqual("Divide by zero error encountered.", events[1].Message);
        Assert.AreEqual(16, events[1].Level);
        Assert.IsTrue(events[1].IsError);
    }

    [TestMethod]
    public async Task Error__Get_without_capture__Will_fail_execution()
    {
        // Arrange
        var phorm = getPhormSession();
        phorm.ExceptionsAsConsoleMessage = false;
        await setConsoleOutputErrorContract(phorm);

        var arg = new ConsoleTest();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<SqlException>
            (async () => await phorm.From<IConsoleTest>(arg).GetAsync<object>(TestContext.CancellationToken));

        // Assert
        Assert.AreEqual("Divide by zero error encountered.", ex.Message);
    }
}