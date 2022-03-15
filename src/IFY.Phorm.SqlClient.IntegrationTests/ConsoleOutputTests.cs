using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class ConsoleOutputTests : SqlIntegrationTestBase
    {
        public interface IPrintTest : IPhormContract
        {
            string? Text { get; }
        }
        public class PrintTest : IPrintTest
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
            using var conn = connProv.GetConnection(null);
            using var cmd = conn.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = @"CREATE OR ALTER PROC [dbo].[usp_PrintTest]
	@Text VARCHAR(256) = NULL
AS
	SET NOCOUNT ON
	RAISERROR ('Before', 0, 1) WITH NOWAIT;
	RAISERROR (@Text, 2, 3) WITH NOWAIT;
	RAISERROR ('After', 4, 5) WITH NOWAIT;
    PRINT 'End'
RETURN 1";
            _ = cmd.ExecuteReaderAsync(CancellationToken.None);
        }

        [TestMethod]
        public void Console_output__Contract_member__Get_all_events_in_order()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv);
            setConsoleOutputContract(connProv);

            var arg = new PrintTest
            {
                Text = DateTime.UtcNow.ToString("o")
            };

            // Act
            var res = phorm.Call<IPrintTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            var evs = arg.ConsoleEvents.Value.ToArray();
            Assert.AreEqual(4, evs.Length);
            Assert.AreEqual("dbo.usp_PrintTest @ 5", evs[0].Source);
            Assert.AreEqual("Before", evs[0].Message);
            Assert.AreEqual(0, evs[0].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 6", evs[1].Source);
            Assert.AreEqual(arg.Text, evs[1].Message);
            Assert.AreEqual(2, evs[1].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 7", evs[2].Source);
            Assert.AreEqual("After", evs[2].Message);
            Assert.AreEqual(4, evs[2].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 8", evs[3].Source);
            Assert.AreEqual("End", evs[3].Message);
            Assert.AreEqual(0, evs[3].Level);
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
            var res = phorm.Call<IPrintTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            var evs = arg.ConsoleEvents.Value.ToArray();
            Assert.AreEqual(4, evs.Length);
            Assert.AreEqual("dbo.usp_PrintTest @ 5", evs[0].Source);
            Assert.AreEqual("Before", evs[0].Message);
            Assert.AreEqual(0, evs[0].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 6", evs[1].Source);
            Assert.AreEqual(arg.Text, evs[1].Message);
            Assert.AreEqual(2, evs[1].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 7", evs[2].Source);
            Assert.AreEqual("After", evs[2].Message);
            Assert.AreEqual(4, evs[2].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 8", evs[3].Source);
            Assert.AreEqual("End", evs[3].Message);
            Assert.AreEqual(0, evs[3].Level);
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
            var res = phorm.Call<IPrintTest>(arg);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(4, events.Count);
            Assert.AreEqual("dbo.usp_PrintTest @ 5", events[0].Source);
            Assert.AreEqual("Before", events[0].Message);
            Assert.AreEqual(0, events[0].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 6", events[1].Source);
            Assert.AreEqual(arg.Text, events[1].Message);
            Assert.AreEqual(2, events[1].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 7", events[2].Source);
            Assert.AreEqual("After", events[2].Message);
            Assert.AreEqual(4, events[2].Level);
            Assert.AreEqual("dbo.usp_PrintTest @ 8", events[3].Source);
            Assert.AreEqual("End", events[3].Message);
            Assert.AreEqual(0, events[3].Level);
        }
    }
}