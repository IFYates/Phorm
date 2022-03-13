using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace IFY.Phorm.SqlClient.IntegrationTests
{
    [TestClass]
    public class SqlIntegrationTests
    {
        [PhormContract(Name = "DataTable")]
        public class DataItem : IUpsert, IUpsertOnlyIntWithId, IUpsertWithId
        {
            public long Id { get; set; }
            [DataMember(Name = "Int")]
            public int? Num { get; set; }
            public string? Text { get; set; }
            public byte[]? Data { get; set; }
            public DateTime? DateTime { get; set; }

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

        [PhormContract(Name = "DataTable")]
        public class DataItemWithoutText
        {
            public long Id { get; set; }
            [DataMember(Name = "Int")]
            public int? Num { get; set; }
            [IgnoreDataMember]
            public string? Text { get; set; }
            public byte[]? Data { get; set; }
            public DateTime? DateTime { get; set; }

            public DataItemWithoutText(long id, int? num, string? text, byte[]? data, DateTime? dateTime)
            {
                Id = id;
                Num = num;
                Text = text;
                Data = data;
                DateTime = dateTime;
            }
            public DataItemWithoutText() : this(default, default, default, default, default)
            { }
        }

        [PhormContract]
        public interface IUpsert : IPhormContract
        {
            [DataMember(Name = "Int")]
            int? Num { get; }
            string? Text { get; }
            byte[]? Data { get; }
            DateTime? DateTime { get; }
        }
        [PhormContract(Name = "Upsert")]
        public interface IUpsertWithId : IUpsert
        {
            long Id { set; }
        }

        [PhormContract]
        public interface IGetAll : IPhormContract
        {
            //long Id { get; }
            //int? Num { get; }
            //string? Text { get; }
            //byte[]? Data { get; }
            //DateTime? DateTime { get; }
        }

        [PhormContract(Name = "Upsert")]
        public interface IUpsertOnlyIntWithId : IPhormContract
        {
            long Id { set; }
            [DataMember(Name = "Int")]
            int? Num { get; }
        }

        private static IPhormSession getPhormSession()
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

        public enum DataType { None, Numeric, String }
        public abstract class BaseGS
        {
            public long Id { get; set; }
            public string Key { get; set; }
            [DataMember(Name = "TypeId")]
            public DataType Type { get; set; }
        }
        [PhormSpecOf(nameof(Type), DataType.Numeric)]
        public class NumGS : BaseGS
        {
            public decimal Number { get; set; }
        }
        [PhormSpecOf("TypeId", 2)]
        public class StringGS : BaseGS
        {
            public string String { get; set; }
        }
        [TestMethod]
        public void GenSpec_Test()
        {
            var phorm = getPhormSession();
            var res = phorm.From("GenSpecTest").Get<GenSpec<BaseGS, NumGS, StringGS>>()!;
            var all = res.All();
            var nums = res.OfType<NumGS>().ToList();
            var strs = res.OfType<StringGS>().ToList();

            Assert.AreEqual(2, all.Length);
            Assert.AreEqual(12.34, nums.Single().Number);
            Assert.AreEqual("Value", strs.Single().String);
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

            var arg = new
            {
                Id = ContractMember.Out<long>()
            };

            // Act
            var res = phorm.Call("Upsert", arg);
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

            var arg = new DataItem();

            // Act
            var res = phorm.Call<IUpsertOnlyIntWithId>(arg);
            var obj = phorm.Get<DataItem>()!;

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
            var x = phorm.From<IGetAll>(obj)
                .Get<DataItem[]>()!;

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
            var res3 = phorm.From<IDataView>(new { obj2.Id })
                .Get<DataItem[]>()!;

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
            var res = phorm.From<IDataView>()
                .Get<DataItem[]>()!;

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.IsTrue(res.All(e => e.Num == 1));
        }

        [TestMethod]
        public void Many__Filtered_from_table()
        {
            var phorm = getPhormSession();

            var res = phorm.Call("Upsert");

            var x = phorm.From<IDataView>(new { Id = 1 })
                .Get<DataItem[]>()!;

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

            var obj = phorm.From<IDataView>(new { Id = 1 })
                .Get<DataItemWithoutText>()!;

            Assert.AreEqual(1, res);
            Assert.IsNull(obj.Text);
        }

        #endregion One
    }
}
