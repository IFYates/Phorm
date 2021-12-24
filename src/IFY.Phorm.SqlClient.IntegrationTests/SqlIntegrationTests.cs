using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class SqlIntegrationTests
    {
        [PhormContract(Name = "DataTable")]
        public record DataItem(long Id, int? Int, string? Text, byte[]? Data, DateTime? DateTime)
            : IUpsert, IUpsertOnlyIntWithId, IUpsertWithId
        {
            public DataItem() : this(default, default, default, default, default)
            { }
        }

        [PhormContract(Name = "DataTable")]
        public record DataItemWithoutText(long Id, int? Int, [property: IgnoreDataMember] string? Text, byte[]? Data, DateTime? DateTime)
        {
            public DataItemWithoutText() : this(default, default, default, default, default)
            { }
        }

        [PhormContract]
        public interface IUpsert : IPhormContract
        {
            int? Int { get; }
            string? Text { get; }
            byte[]? Data { get; }
            DateTime? DateTime { get; }
        }
        [PhormContract(Name = "Upsert")]
        public interface IUpsertWithId : IUpsert
        {
            long Id { init; }
        }

        [PhormContract]
        public interface IGetAll : IPhormContract
        {
            //long Id { get; }
            //int? Int { get; }
            //string? Text { get; }
            //byte[]? Data { get; }
            //DateTime? DateTime { get; }
        }

        [PhormContract(Name = "Upsert")]
        public interface IUpsertOnlyIntWithId : IPhormContract
        {
            long Id { init; }
            int? Int { get; }
        }

        private static SqlPhormSession getPhormSession()
        {
            var connProc = new SqlConnectionProvider(@"Server=(localdb)\ProjectModels;Database=PhormTests;");

            var phorm = new SqlPhormSession(connProc, "*");

            phorm.Call("ClearTable");

            return phorm;
        }

        [PhormContract(Name = "Data", Target = DbObjectType.View)]
        public interface IDataView : IPhormContract
        {
            long? Id { get; }
        }

        #region Call

        [TestMethod]
        public void Call__By_anon_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.Call("Upsert", new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Int);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_and_anon_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();

            var randNum = DateTime.UtcNow.Millisecond;
            var randStr = Guid.NewGuid().ToString();
            var randData = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());
            var randDT = DateTime.UtcNow;

            // Act
            var res = phorm.Call<IUpsert>(new { Int = randNum, Text = randStr, Data = randData, DateTime = randDT });
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(randNum, obj.Int);
            Assert.AreEqual(randStr, obj.Text);
            CollectionAssert.AreEqual(randData, obj.Data);
            Assert.AreEqual(randDT, obj.DateTime);
        }

        [TestMethod]
        public void Call__By_contract_arg_Insert_various_types()
        {
            // Arrange
            var phorm = getPhormSession();

            var arg = new DataItem(0,
                DateTime.UtcNow.Millisecond,
                Guid.NewGuid().ToString(),
                Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()),
                DateTime.UtcNow
            );

            // Act
            var res = phorm.Call<IUpsert>(arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(arg.Int, obj.Int);
            Assert.AreEqual(arg.Text, obj.Text);
            CollectionAssert.AreEqual(arg.Data, obj.Data);
            Assert.AreEqual(arg.DateTime, obj.DateTime);
        }

        [TestMethod]
        public void Call__Get_by_anon_output()
        {
            // Arrange
            var phorm = getPhormSession();

            var arg = new
            {
                Id = ContractMember.Out<long>()
            };

            // Act
            var res = phorm.Call("Upsert", arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id.Value);
        }

        [TestMethod]
        public void Call__Get_by_contract_output()
        {
            // Arrange
            var phorm = getPhormSession();

            var arg = new DataItem();

            // Act
            var res = phorm.Call<IUpsertOnlyIntWithId>(arg);
            var obj = phorm.From("DataTable", objectType: DbObjectType.Table).One<DataItem>()!;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(obj.Id, arg.Id);
        }

        #endregion Call

        #region Many

        [TestMethod]
        public void Many__Can_access_returnvalue_of_sproc()
        {
            var phorm = getPhormSession();

            phorm.Call("Upsert");
            phorm.Call("Upsert");
            phorm.Call("Upsert");
            phorm.Call("Upsert");

            var obj = new { ReturnValue = ContractMember.RetVal() };
            var x = phorm.From<IGetAll>().Many<DataItem>(obj);

            Assert.AreEqual(1, obj.ReturnValue.Value);
            Assert.AreEqual(4, x.Length);
        }

        [TestMethod]
        public void Many__Filtered_from_view()
        {
            // Arrange
            var phorm = getPhormSession();

            var obj1 = new DataItem();
            var res1 = phorm.Call<IUpsertWithId>(obj1);

            var obj2 = new DataItem();
            var res2 = phorm.Call<IUpsertWithId>(obj2);

            // Act
            var res3 = phorm.From<IDataView>().Many<DataItem>(new { obj2.Id });

            // Assert
            Assert.AreEqual(1, res1);
            Assert.AreEqual(1, res2);
            Assert.AreNotEqual(obj1.Id, obj2.Id);
            Assert.AreEqual(obj2.Id, res3.Single().Id);
        }

        [TestMethod]
        public void Many__Filtered_by_view()
        {
            // Arrange
            var phorm = getPhormSession();

            phorm.Call("Upsert", new { Int = 0, IsInView = false });
            phorm.Call("Upsert", new { Int = 0, IsInView = false });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });
            phorm.Call("Upsert", new { Int = 1, IsInView = true });

            // Act
            var res = phorm.From<IDataView>().Many<DataItem>();

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.IsTrue(res.All(e => e.Int == 1));
        }

        [TestMethod]
        public void Many__Filtered_from_table()
        {
            var phorm = getPhormSession();

            var res = phorm.Call("Upsert");

            var x = phorm.From<IDataView>().Many<DataItem>(new { Id = 1 });

            Assert.AreEqual(1, res);
            Assert.AreEqual(1, x.Single().Id);
        }

        #endregion Many

        #region One

        [TestMethod]
        public void One__Can_ignore_property()
        {
            var phorm = getPhormSession();

            var res = phorm.Call("Upsert");

            var obj = phorm.From<IDataView>().One<DataItemWithoutText>(new { Id = 1 })!;

            Assert.AreEqual(1, res);
            Assert.IsNull(obj.Text);
        }

        #endregion One

        [TestMethod]
        public void Can_get_print_messages_by_anon()
        {
            var phorm = getPhormSession();

            var randStr = DateTime.Now.ToString("o");

            var arg = new
            {
                Text = randStr,
                Print = ContractMember.Console()
            };

            var res = phorm.Call("PrintTest", arg);

            Assert.AreEqual(1, res);
            Assert.AreEqual(randStr, arg.Print.Value?.Trim());
        }

        [TestMethod]
        public async Task Can_get_error_message_by_anon()
        {
            var phorm = getPhormSession();

            var randStr = DateTime.Now.ToString("o");

            var arg = new
            {
                Text = randStr,
                Print = ContractMember.Console()
            };

            var res = await phorm.CallAsync("ErrorTest", arg);

            Assert.AreEqual(1, res);
            Assert.AreEqual(randStr, arg.Print.Value?.Trim());
        }
    }
}
