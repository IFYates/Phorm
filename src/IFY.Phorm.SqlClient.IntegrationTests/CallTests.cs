using IFY.Phorm.Connectivity;
using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;
using System.Text;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class CallTests : SqlIntegrationTestBase
    {
        [PhormContract(Name = "CallTestTable", Target = DbObjectType.Table)]
        class DataItem : IUpsert, IUpsertOnlyIntWithId
        {
            public long Id { get; set; } = 0;
            [DataMember(Name = "Int")]
            public int? Num { get; set; } = null;
            public string? Text { get; set; } = null;
            public byte[]? Data { get; set; } = null;
            public DateTime? DateTime { get; set; } = null;

            public DataItem(long id, int? num, string? text, byte[]? data, DateTime? dateTime)
            {
                Id = id;
                Num = num;
                Text = text;
                Data = data;
                DateTime = dateTime;
            }
            public DataItem() : this(default, default, default, default, default)
            { }
        }

        [PhormContract(Name = "CallTest_Upsert")]
        interface IUpsert : IPhormContract
        {
            [DataMember(Name = "Int")]
            int? Num { get; }
            string? Text { get; }
            byte[]? Data { get; }
            DateTime? DateTime { get; }
        }

        [PhormContract(Name = "CallTest_Upsert")]
        interface IUpsertOnlyIntWithId : IPhormContract
        {
            long Id { set; }
            [DataMember(Name = "Int")]
            int? Num { get; }
        }

        private void setupCallTestSchema(AbstractPhormSession phorm)
        {
            SqlTestHelpers.ApplySql(phorm, @"DROP TABLE IF EXISTS [dbo].[CallTestTable]");
            SqlTestHelpers.ApplySql(phorm, @"CREATE TABLE [dbo].[CallTestTable] (
	[Id] BIGINT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Int] INT NULL,
	[Text] VARCHAR(256) NULL,
	[Data] VARBINARY(MAX) NULL,
	[DateTime] DATETIME2 NULL,
	[IsInView] BIT NOT NULL DEFAULT (1)
)");

            SqlTestHelpers.ApplySql(phorm, @"CREATE OR ALTER PROC [dbo].[usp_CallTest_Upsert]
	@Id BIGINT = NULL OUTPUT,
	@Int INT = NULL,
	@Text VARCHAR(256) = NULL,
	@Data VARBINARY(MAX) = NULL,
	@DateTime DATETIME2 = NULL,
	@IsInView BIT = NULL
AS
	SET NOCOUNT ON
	IF (@Id IS NULL) BEGIN
		INSERT [dbo].[CallTestTable] ([Int], [Text], [Data], [DateTime], [IsInView])
			SELECT @Int, @Text, @Data, @DateTime, ISNULL(@IsInView, 1)
		SET @Id = SCOPE_IDENTITY()
		RETURN 1
	END

	UPDATE [dbo].[CallTestTable] SET
		[Int] = @Int,
		[Text] = @Text,
		[Data] = @Data,
		[DateTime] = @DateTime,
		[IsInView] = ISNULL(@IsInView, [IsInView])
		WHERE [Id] = @Id
RETURN @@ROWCOUNT");
        }

        [TestMethod]
        public void Call__By_anon_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();
            setupCallTestSchema(phorm);

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.Call("CallTest_Upsert", new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.Get<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Num);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_and_anon_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();
            setupCallTestSchema(phorm);

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.Call<IUpsert>(new { Num = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.Get<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Num);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();
            setupCallTestSchema(phorm);

            var arg = new DataItem(0,
                DateTime.UtcNow.Millisecond,
                Guid.NewGuid().ToString(),
                Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                DateTime.UtcNow
            );

            // Act
            var res = phorm.Call<IUpsert>(arg);
            var obj = phorm.Get<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(arg.Num, obj.Num);
            Assert.AreEqual(arg.Text, obj.Text);
            CollectionAssert.AreEqual(arg.Data, obj.Data);
            Assert.AreEqual(arg.DateTime, obj.DateTime);
        }

        [TestMethod]
        public void Call__Get_by_anon_output()
        {
            // Arrange
            var phorm = getPhormSession();
            setupCallTestSchema(phorm);

            var arg = new
            {
                Id = ContractMember.Out<long>()
            };

            // Act
            var res = phorm.Call("CallTest_Upsert", arg);
            var obj = phorm.Get<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id.Value);
        }

        [TestMethod]
        public void Call__Get_by_contract_output()
        {
            // Arrange
            var phorm = getPhormSession();
            setupCallTestSchema(phorm);

            var arg = new DataItem();

            // Act
            var res = phorm.Call<IUpsertOnlyIntWithId>(arg);
            var obj = phorm.Get<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id);
        }
    }
}