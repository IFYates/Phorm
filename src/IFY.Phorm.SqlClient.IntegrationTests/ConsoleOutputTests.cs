using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
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

            public ContractMember<ConsoleMessage[]> ConsoleEvents { get; set; } = ContractMember.Console();
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

        private static void setConsoleOutputContract(IPhormDbConnectionProvider connProv)
        {
            SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_ConsoleTest]
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	RAISERROR ('Before', 0, 1) WITH NOWAIT;
	RAISERROR (@Text, 2, 3) WITH NOWAIT;
	RAISERROR ('After', 4, 5) WITH NOWAIT;
    PRINT 'End'
RETURN 1");
        }
        private static void setConsoleOutputErrorContract(IPhormDbConnectionProvider connProv)
        {
            SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_ConsoleTest]
AS
	SET NOCOUNT ON
	RAISERROR ('Before', 1, 1) WITH NOWAIT;
	SELECT 1 / 0
	RAISERROR ('After', 1, 1) WITH NOWAIT;
    PRINT 'End'
RETURN 1");
        }

        [TestMethod]
        public void Console_output__Contract_member__Get_all_events_in_order()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setConsoleOutputContract(connProv);

            var arg = new ConsoleTest
            {
                Text = DateTime.UtcNow.ToString("o")
            };

            // Act
            var res = phorm.Call<IConsoleTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            var events = arg.ConsoleEvents.Value.ToArray();
            Assert.AreEqual(4, events.Length);
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
        public void Console_output__Anonymous_contract_member__Get_all_events_in_order()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setConsoleOutputContract(connProv);

            var arg = new
            {
                Text = DateTime.UtcNow.ToString("o"),
                ConsoleEvents = ContractMember.Console()
            };

            // Act
            var res = phorm.Call<IConsoleTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            var events = arg.ConsoleEvents.Value.ToArray();
            Assert.AreEqual(4, events.Length);
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
        public void Console_output__Event__Get_all_events_in_order(bool asGlobal)
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setConsoleOutputContract(connProv);

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
            var res = phorm.Call<IConsoleTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(4, events.Count);
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
        public void Error__Call__Can_receive_error_info_as_message()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            phorm.ErrorsAsConsoleMessage = true;
            setConsoleOutputErrorContract(connProv);

            var arg = new ConsoleTest();

            // Act
            var res = phorm.Call<IConsoleTest>(arg);

            // Assert
            Assert.AreEqual(0, res);
            var events = arg.ConsoleEvents.Value;
            Assert.AreEqual(2, events.Length);
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
        public void Error__Call_without_capture__Will_fail_execution()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            phorm.ErrorsAsConsoleMessage = false;
            setConsoleOutputErrorContract(connProv);

            var arg = new ConsoleTest();

            // Act
            var ex = Assert.ThrowsException<SqlException>(() =>
            {
                _ = phorm.Call<IConsoleTest>(arg);
            });

            // Assert
            Assert.AreEqual("Divide by zero error encountered.", ex.Message);
        }

        [TestMethod]
        public void Error__Get__Can_receive_error_info_as_message()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            phorm.ErrorsAsConsoleMessage = true;
            setConsoleOutputErrorContract(connProv);

            var arg = new
            {
                ConsoleEvents = ContractMember.Console(),
                ReturnValue = ContractMember.RetVal()
            };

            // Act
            _ = phorm.From<IConsoleTest>(arg).Get<object>();

            // Assert
            Assert.AreEqual(0, arg.ReturnValue.Value);
            var events = arg.ConsoleEvents.Value;
            Assert.AreEqual(2, events.Length);
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
        public void Error__Get_without_capture__Will_fail_execution()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            phorm.ErrorsAsConsoleMessage = false;
            setConsoleOutputErrorContract(connProv);

            var arg = new ConsoleTest();

            // Act
            var ex = Assert.ThrowsException<SqlException>(() =>
            {
                _ = phorm.From<IConsoleTest>(arg).Get<object>();
            });

            // Assert
            Assert.AreEqual("Divide by zero error encountered.", ex.Message);
        }
    }
}