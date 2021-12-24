using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Data.Tests
{
    [TestClass]
    public class ContractMemberTests
    {
        public class TestAttribute : Attribute, IContractMemberAttribute
        {
            public object? Context { get; private set; }

            public void SetContext(object? context)
            {
                Context = context;
            }
        }

        [Test]
        public string? StringProperty { get; set; }
        public int IntProperty { get; set; }
        public int? NullableIntProperty { get; set; }

        [TestMethod]
        public void In__Without_property()
        {
            var res = ContractMember.In("name", typeof(string), null);

            Assert.AreEqual("name", res.Name);
            Assert.AreEqual(typeof(string), res.Value);
            Assert.IsFalse(res.HasChanged);
            Assert.AreEqual(ParameterType.Input, res.Direction);
            Assert.IsNull(res.SourceProperty);
            Assert.AreEqual(typeof(Type), res.ValueType);
        }

        [TestMethod]
        public void InOut__Without_property()
        {
            var res = ContractMember.InOut("name", typeof(string), null);

            Assert.AreEqual("name", res.Name);
            Assert.AreEqual(typeof(string), res.Value);
            Assert.IsFalse(res.HasChanged);
            Assert.AreEqual(ParameterType.InputOutput, res.Direction);
            Assert.IsNull(res.SourceProperty);
            Assert.AreEqual(typeof(Type), res.ValueType);
        }

        [TestMethod]
        public void Out__Without_args()
        {
            var res = ContractMember.Out<Type>();

            Assert.AreEqual(string.Empty, res.Name);
            Assert.IsNull(res.Value);
            Assert.IsFalse(res.HasChanged);
            Assert.AreEqual(ParameterType.Output, res.Direction);
            Assert.IsNull(res.SourceProperty);
            Assert.AreEqual(typeof(Type), res.ValueType);
        }

        [TestMethod]
        public void Out__Without_property()
        {
            var res = ContractMember.Out<Type>("name", null);

            Assert.AreEqual("name", res.Name);
            Assert.IsNull(res.Value);
            Assert.IsFalse(res.HasChanged);
            Assert.AreEqual(ParameterType.Output, res.Direction);
            Assert.IsNull(res.SourceProperty);
            Assert.AreEqual(typeof(Type), res.ValueType);
        }

        [TestMethod]
        public void RetVal()
        {
            var res = ContractMember.RetVal();

            Assert.AreEqual("return", res.Name);
            Assert.AreEqual(0, res.Value);
            Assert.IsFalse(res.HasChanged);
            Assert.AreEqual(ParameterType.ReturnValue, res.Direction);
            Assert.IsNull(res.SourceProperty);
            Assert.AreEqual(typeof(int), res.ValueType);
        }

        [TestMethod]
        public void ResolveAttributes__Sets_context()
        {
            var prop = GetType().GetProperty(nameof(StringProperty)) ?? throw new Exception();

            var obj = new object();

            var memb = ContractMember.In(prop.Name, string.Empty, prop);

            memb.ResolveAttributes(obj, out var isSecure);

            Assert.IsFalse(isSecure);
            Assert.AreSame(obj, ((TestAttribute)memb.Attributes.Single()).Context);
        }

        [TestMethod]
        public void ResolveAttributes__Has_secure_attribute()
        {
            var prop = GetType().GetProperty(nameof(SecureDataProperty)) ?? throw new Exception();

            var obj = new object();

            var memb = ContractMember.In(prop.Name, string.Empty, prop);

            memb.ResolveAttributes(obj, out var isSecure);

            Assert.IsTrue(isSecure);
            Assert.AreSame(typeof(TestSecureAttribute), memb.Attributes.Single().GetType());
        }

        #region GetMembersFromContract

        class ObjectWithReturnValueProperty
        {
            public ContractMember ReturnValue { get; } = ContractMember.RetVal();
        }

        [TestMethod]
        public void GetMembersFromContract__ReturnValue_property_on_object()
        {
            // Arrange
            var obj = new ObjectWithReturnValueProperty();

            // Act
            var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract));

            // Assert
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual(ParameterType.ReturnValue, ((ContractMember<int>)res[0]).Direction);
            Assert.AreSame(obj.ReturnValue, (ContractMember<int>)res[0]);
        }

        #endregion GetMembersFromContract

        #region ToDataParameter

        public enum TestEnum
        {
            Value1 = 1,
            Value2 = 2
        }

        [DataMember(IsRequired = true)]
        public string? RequiredProperty { get; set; }

        private static void getDbMocks(out Mock<IAsyncDbCommand> cmdMock, out Mock<IDbDataParameter> dbpMock)
        {
            dbpMock = new Mock<IDbDataParameter>(MockBehavior.Strict);
            dbpMock.SetupProperty(m => m.ParameterName);
            dbpMock.SetupProperty(m => m.Direction);
            dbpMock.SetupProperty(m => m.Size);
            dbpMock.SetupProperty(m => m.DbType);
            dbpMock.SetupProperty(m => m.Value);

            cmdMock = new Mock<IAsyncDbCommand>(MockBehavior.Strict);
            cmdMock.Setup(m => m.CreateParameter())
                .Returns(dbpMock.Object);
        }

        [TestMethod]
        public void ToDataParameter__No_property()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var memb = ContractMember.In("Name", "value");

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(dbpMock.Object, dbp);
            Assert.AreEqual("@Name", dbp.ParameterName);
            Assert.AreEqual(ParameterDirection.Input, dbp.Direction);
            Assert.AreEqual(0, ((IDbDataParameter)dbp).Size);
            Assert.AreEqual(DbType.AnsiString, dbp.DbType);
            Assert.AreEqual("value", dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__From_property()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(StringProperty));

            var memb = ContractMember.In("Name", "value", prop);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(dbpMock.Object, dbp);
            Assert.AreEqual("@Name", dbp.ParameterName);
            Assert.AreEqual(ParameterDirection.Input, dbp.Direction);
            Assert.AreEqual(0, ((IDbDataParameter)dbp).Size);
            Assert.AreEqual(DbType.AnsiString, dbp.DbType);
            Assert.AreEqual("value", dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Null_sends_as_DBNull()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var memb = ContractMember.In<string?>("Name", null);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(256, ((IDbDataParameter)dbp).Size);
            Assert.AreEqual(DbType.AnsiString, dbp.DbType);
            Assert.AreEqual(DBNull.Value, dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Transphorms_value()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(TransphormedStringProperty));

            var memb = ContractMember.In("Name", "value", prop);
            memb.ResolveAttributes(null, out _);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual("ToDatasource_value", dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Enum_sent_as_int()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var memb = ContractMember.In("Name", TestEnum.Value1);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.Int32, dbp.DbType);
            Assert.AreEqual(1, dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__DateTime()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var dt = DateTime.UtcNow;
            var memb = ContractMember.In("Name", dt);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.DateTime2, dbp.DbType);
            Assert.AreEqual(dt, dbp.Value);
        }

        [TestMethod]
        [DataRow("0001-01-01 00:00:00")]
        [DataRow("1000-01-01 00:00:00")]
        [DataRow("1753-01-01 00:00:00")]
        public void ToDataParameter__DateTime_caps_at_db_minimum(string dtStr)
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var dt = DateTime.Parse(dtStr);
            var memb = ContractMember.In("Name", dt);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.DateTime2, dbp.DbType);
            Assert.AreEqual(SqlDateTime.MinValue.Value, dbp.Value);
        }

        [TestMethod]
        [DataRow("9999-12-31 23:59:59.999999")]
        public void ToDataParameter__DateTime_caps_at_db_maximum(string dtStr)
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var dt = DateTime.Parse(dtStr);
            var memb = ContractMember.In("Name", dt);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.DateTime2, dbp.DbType);
            Assert.AreEqual(SqlDateTime.MaxValue.Value, dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Guid()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var val = Guid.NewGuid();
            var memb = ContractMember.In("Name", val);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.Guid, dbp.DbType);
            Assert.AreEqual(val, dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Outbound_binary_data()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var memb = ContractMember.Out<byte[]>();

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(ParameterDirection.Output, dbp.Direction);
            Assert.AreEqual(DbType.Binary, dbp.DbType);
            Assert.AreEqual(DBNull.Value, dbp.Value);
        }

        [TestMethod]
        public void ToDataParameter__Required_parameter_null__Exception()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(RequiredProperty)) ?? throw new Exception();

            var memb = ContractMember.In<string?>(prop.Name, null, prop);
            memb.ResolveAttributes(null, out _);

            // Act
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                memb.ToDataParameter(cmdMock.Object);
            });

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
        }

        [TestMethod]
        public void ToDataParameter__Required_parameter_not_null__OK()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(RequiredProperty)) ?? throw new Exception();

            var memb = ContractMember.In<string?>(prop.Name, "value", prop);
            memb.ResolveAttributes(null, out _);

            // Act
            var res = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual("value", res.Value);
        }

        [TestMethod]
        public void ToDataParameter__Required_parameter_default__OK()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(RequiredProperty)) ?? throw new Exception();

            var memb = ContractMember.In(prop.Name, 0, prop);
            memb.ResolveAttributes(null, out _);

            // Act
            var res = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(0, res.Value);
        }

        [TestMethod]
        public void ToDataParameter__Encrypts_secure_value()
        {
            // Arrange
            getDbMocks(out var cmdMock, out var dbpMock);

            var prop = GetType().GetProperty(nameof(SecureDataProperty));

            var memb = ContractMember.In("Name", new byte[] { 0 }, prop);
            memb.ResolveAttributes(null, out _);

            // Act
            var dbp = memb.ToDataParameter(cmdMock.Object);

            // Assert
            cmdMock.Verify();
            dbpMock.Verify();
            Assert.AreEqual(DbType.Binary, dbp.DbType);
            Assert.AreEqual(1, ((IDbDataParameter)dbp).Size);
            CollectionAssert.AreEqual(new byte[] { 2 }, (byte[]?)dbp.Value);
        }

        #endregion ToDataParameter

        #region FromDatasource

        public class TestTransphormAttribute : AbstractTransphormAttribute
        {
            public override object? FromDatasource(Type type, object? data)
            {
                return "FromDatasource_" + data;
            }

            public override object? ToDatasource(object? data)
            {
                return "ToDatasource_" + data;
            }
        }

        [TestTransphorm]
        public string? TransphormedStringProperty { get; set; }

        public class TestSecureAttribute : AbstractSecureValueAttribute
        {
            public override byte[] Decrypt(byte[]? value)
            {
                return new byte[] { 1 };
            }

            public override byte[] Encrypt(object? value)
            {
                return new byte[] { 2 };
            }
        }

        [TestSecure]
        public byte[]? SecureDataProperty { get; set; }

        [TestMethod]
        public void FromDatasource__DBNull_is_null()
        {
            var memb = ContractMember.Out<string>();

            memb.FromDatasource(DBNull.Value);

            Assert.IsNull(memb.Value);
        }

        [TestMethod]
        public void FromDatasource__Type_changed_to_T()
        {
            var memb = ContractMember.Out<int>();

            memb.FromDatasource("1234");

            Assert.AreEqual(1234, memb.Value);
            Assert.IsTrue(memb.HasChanged);
        }

        [TestMethod]
        public void FromDatasource__Transphorms_value()
        {
            var prop = GetType().GetProperty(nameof(TransphormedStringProperty));

            var memb = ContractMember.Out<object>(string.Empty, prop);
            memb.ResolveAttributes(null, out _);

            memb.FromDatasource("Value");

            Assert.AreEqual("FromDatasource_Value", memb.Value);
        }

        [TestMethod]
        public void FromDatasource__Decrypts_value()
        {
            var prop = GetType().GetProperty(nameof(SecureDataProperty));

            var memb = ContractMember.Out<byte[]>(string.Empty, prop);
            memb.ResolveAttributes(null, out _);

            memb.FromDatasource(new byte[] { 0 });

            CollectionAssert.AreEqual(new byte[] { 1 }, memb.Value);
        }

        #endregion FromDatasource

        [TestMethod]
        [DataRow(typeof(ContractMember<int>), "12345", 12345)]
        [DataRow(typeof(ContractMember<int?>), "12345", 12345)]
        [DataRow(typeof(ContractMember<double>), "12.34", 12.34)]
        public void SetValue__Converts_value_to_type_T(Type memberType, string value, object exp)
        {
            var c = memberType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            var member = (ContractMember)c[0].Invoke(new object?[] { "", null, ParameterType.Input });

            member.SetValue(value);

            Assert.AreEqual(exp, member.Value);
        }

        [TestMethod]
        [DataRow(nameof(IntProperty), "12345", 12345)]
        [DataRow(nameof(NullableIntProperty), "12345", 12345)]
        public void SetValue__Converts_value_to_property_type(string propertyName, string value, object exp)
        {
            var prop = GetType().GetProperty(propertyName);
            var member = new ContractMember<object>(propertyName, null, ParameterType.Input, prop);

            member.SetValue(value);

            Assert.AreEqual(exp, member.Value);
        }
    }
}