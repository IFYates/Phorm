using IFY.Phorm.Connectivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class ConnectionTests : SqlIntegrationTestBase
    {
        public class ContextTest
        {
            public string? Context { get; set; }
        }

        private static void setContextTestContract(IPhormDbConnectionProvider connProv)
        {
            SqlTestHelpers.ApplySql(connProv, @"CREATE OR ALTER PROC [dbo].[usp_ContextTest]
AS
	SET NOCOUNT ON
    SELECT APP_NAME() [Context]
RETURN 1");
        }

        [TestMethod]
        public void Console_output__Contract_member__Get_all_events_in_order()
        {
            // Arrange
            var phorm = getPhormSession(out var connProv, "TestContext");
            setContextTestContract(connProv);

            // Act
            var res = phorm.From("ContextTest").Get<ContextTest>()!;

            // Assert
            Assert.AreEqual("TestContext", res.Context);
        }
    }
}