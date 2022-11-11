using IFY.Phorm.Encryption;
using IFY.Phorm.Transformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Data.Tests;

// TODO: sort out ContractMemberDefinition correctly
[TestClass]
public class ContractMemberTests
{
    public string StringProperty { get; set; } = string.Empty;
    public int IntProperty { get; set; }
    public int? NullableIntProperty { get; set; }

    [TestMethod]
    public void InOut()
    {
        var res = ContractMember.InOut("test");

        Assert.AreEqual(string.Empty, res.DbName);
        Assert.AreEqual("test", res.Value);
        Assert.IsFalse(res.HasChanged);
        Assert.AreEqual(ParameterType.InputOutput, res.Direction);
        Assert.IsNull(res.SourceMember);
        Assert.AreEqual(typeof(string), res.ValueType);
    }

    [TestMethod]
    public void Out()
    {
        var res = ContractMember.Out<Type>();

        Assert.AreEqual(string.Empty, res.DbName);
        Assert.IsNull(res.Value);
        Assert.IsFalse(res.HasChanged);
        Assert.AreEqual(ParameterType.Output, res.Direction);
        Assert.IsNull(res.SourceMember);
        Assert.AreEqual(typeof(Type), res.ValueType);
    }

    [TestMethod]
    public void RetVal()
    {
        var res = ContractMember.RetVal();

        Assert.AreEqual("return", res.DbName);
        Assert.AreEqual(0, res.Value);
        Assert.IsFalse(res.HasChanged);
        Assert.AreEqual(ParameterType.ReturnValue, res.Direction);
        Assert.IsNull(res.SourceMember);
        Assert.AreEqual(typeof(int), res.ValueType);
    }

    #region GetMembersFromContract

    [ExcludeFromCodeCoverage]
    class ObjectWithMethodMember
    {
        public string Value1
        {
            [ContractMember]
            get; // Must not be picked up separately
            [ContractMember]
            set; // Must not be picked up separately
        } = "A";
#pragma warning disable CA1822 // Mark members as static
        [ContractMember]
        public string Value2() => "B";
        public string Value3(int arg) => "C" + arg; // Ignored
#pragma warning restore CA1822 // Mark members as static
    }

    [TestMethod]
    public void GetMembersFromContract__Includes_decorated_methods()
    {
        // Act
        var res = ContractMember.GetMembersFromContract(new ObjectWithMethodMember(), typeof(ObjectWithMethodMember), false);

        // Assert
        Assert.AreEqual(2, res.Length);
        Assert.AreEqual("Value1", res[0].DbName);
        Assert.AreEqual("A", res[0].Value);
        Assert.AreEqual("Value2", res[1].DbName);
        Assert.AreEqual("B", res[1].Value);
    }

    [ExcludeFromCodeCoverage]
    class ObjectWithBadMethodMember
    {
        public string Value1 { get; set; } = "A";
#pragma warning disable CA1822 // Mark members as static
        [ContractMember]
        public string Value2(int arg) => "B" + arg;
#pragma warning restore CA1822 // Mark members as static
    }

    [TestMethod]
    public void GetMembersFromContract__Method_has_parameter__Fail()
    {
        // Act
        var ex = Assert.ThrowsException<InvalidDataContractException>
            (() => ContractMember.GetMembersFromContract(null, typeof(ObjectWithBadMethodMember), false));

        // Assert
        Assert.IsTrue(ex.Message.Contains("'IFY.Phorm.Data.Tests.ContractMemberTests+ObjectWithBadMethodMember.Value2'"), "Actual: " + ex.Message);
    }

    class ObjectWithoutReturnValueProperty
    {
    }
    class ObjectWithReturnValueProperty
    {
        public ContractMember ReturnValue { get; } = ContractMember.RetVal();
    }

    [TestMethod]
    public void GetMembersFromContract__ConsoleLogMember_ignored()
    {
        // Arrange
        var obj = new
        {
            Text = "Abcd",
            ConsoleLogMember = new ConsoleLogMember()
        };

        // Act
        var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);

        // Assert
        Assert.AreEqual(1, res.Length);
        Assert.AreEqual("Text", res[0].DbName);
    }

    [TestMethod]
    public void GetMembersFromContract__withReturnValue__ReturnValue_property_added_to_object()
    {
        // Arrange
        var obj = new ObjectWithoutReturnValueProperty();

        // Act
        var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), true);

        // Assert
        Assert.AreEqual(1, res.Length);
        Assert.AreEqual(ParameterType.ReturnValue, res[0].Direction);
    }

    [TestMethod]
    public void GetMembersFromContract__Not_withReturnValue__ReturnValue_property_not_on_object()
    {
        // Arrange
        var obj = new ObjectWithoutReturnValueProperty();

        // Act
        var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);

        // Assert
        Assert.AreEqual(0, res.Length);
    }

    [TestMethod]
    public void GetMembersFromContract__withReturnValue__Uses_existing_ReturnValue_property()
    {
        // Arrange
        var obj = new ObjectWithReturnValueProperty();

        // Act
        var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), true);

        // Assert
        Assert.AreEqual(1, res.Length);
        Assert.AreEqual(ParameterType.ReturnValue, res[0].Direction);
        Assert.AreSame(obj.ReturnValue, res[0]);
    }

    [TestMethod]
    public void GetMembersFromContract__Not_withReturnValue__Uses_existing_ReturnValue_property()
    {
        // Arrange
        var obj = new ObjectWithReturnValueProperty();

        // Act
        var res = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);

        // Assert
        Assert.AreEqual(1, res.Length);
        Assert.AreEqual(ParameterType.ReturnValue, res[0].Direction);
        Assert.AreSame(obj.ReturnValue, res[0]);
    }

    [TestMethod]
    public void ResolveContract__Caches_members_by_type()
    {
        // Act
        var res1 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), typeof(ObjectWithDynamicProperty));
        var res2 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), typeof(ObjectWithDynamicProperty));
        var res3 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), typeof(ObjectWithDynamicProperty));

        // Assert
        Assert.AreSame(res1, res2);
        Assert.AreSame(res1, res3);
    }

    [TestMethod]
    public void ResolveContract__Caches_anon_members_by_type()
    {
        // Arrange
        var obj1 = new { Test = 1 };
        var obj2 = new { Test = 2 };
        var obj3 = new { Test = 3 };

        // Act
        var res1 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), obj1.GetType());
        var res2 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), obj2.GetType());
        var res3 = ContractMemberDefinition.GetFromContract(typeof(IPhormContract), obj3.GetType());

        // Assert
        Assert.AreSame(res1, res2);
        Assert.AreSame(res1, res3);
    }

    class ObjectWithDynamicProperty
    {
        public string Name { get; set; } = string.Empty;
        public int Value1 => _value1++;
        private int _value1 = 0;
    }

    [TestMethod]
    public void GetMembersFromContract__Resolves_members_each_time()
    {
        // Arrange
        var obj = new ObjectWithDynamicProperty();

        // Act
        obj.Name = "A";
        var res1 = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);
        obj.Name = "B";
        var res2 = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);
        obj.Name = "C";
        var res3 = ContractMember.GetMembersFromContract(obj, typeof(IPhormContract), false);

        // Assert
        Assert.AreEqual(3, obj.Value1);
        Assert.AreEqual("A", res1.Single(m => m.DbName == nameof(ObjectWithDynamicProperty.Name)).Value);
        Assert.AreEqual("B", res2.Single(m => m.DbName == nameof(ObjectWithDynamicProperty.Name)).Value);
        Assert.AreEqual("C", res3.Single(m => m.DbName == nameof(ObjectWithDynamicProperty.Name)).Value);
    }

    #endregion GetMembersFromContract

    #region ToDataParameter

    public enum TestEnum
    {
        Value1 = 1,
        Value2 = 2
    }

    [DataMember(IsRequired = true)]
    public string RequiredString { get; set; } = string.Empty;
    [DataMember(IsRequired = true)]
    public int RequiredNumber { get; set; } = 0;

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

        var memb = new ContractMember("Name", "value", ParameterType.Input, typeof(string));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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

        var prop = GetType().GetProperty(nameof(StringProperty))!;

        var memb = new ContractMember("Name", "value", ParameterType.Input, prop);

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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

        var memb = new ContractMember("Name", null, ParameterType.Input, typeof(string));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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

        var prop = GetType().GetProperty(nameof(TransphormedStringProperty))!;

        var memb = new ContractMember("Name", "value", ParameterType.Input, prop);

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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

        var memb = new ContractMember("Name", TestEnum.Value1, ParameterType.Input, typeof(TestEnum));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual(DbType.Int32, dbp.DbType);
        Assert.AreEqual(1, dbp.Value);
    }

#if NET6_0_OR_GREATER
    [TestMethod]
    public void ToDataParameter__DateOnly()
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var dt = DateOnly.FromDateTime(DateTime.Today);
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateOnly));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual(DbType.Date, dbp.DbType);
        Assert.AreEqual(dt, dbp.Value);
    }

    [TestMethod]
    [DataRow("0001-01-01")]
    [DataRow("1000-01-01")]
    [DataRow("1753-01-01")]
    public void ToDataParameter__DateOnly__DateTime_caps_at_db_minimum(string dtStr)
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var exp = DateOnly.FromDateTime(SqlDateTime.MinValue.Value);

        var dt = DateOnly.Parse(dtStr);
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateOnly));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual(DbType.Date, dbp.DbType);
        Assert.AreEqual(exp, dbp.Value);
    }

    [TestMethod]
    [DataRow("9999-12-31")]
    public void ToDataParameter__DateOnly__DateTime_caps_at_db_maximum(string dtStr)
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var exp = DateOnly.FromDateTime(SqlDateTime.MaxValue.Value);

        var dt = DateOnly.Parse(dtStr);
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateOnly));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual(DbType.Date, dbp.DbType);
        Assert.AreEqual(exp, dbp.Value);
    }
#endif

    [TestMethod]
    public void ToDataParameter__DateTime()
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var dt = DateTime.UtcNow;
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateTime));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateTime));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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
        var memb = new ContractMember("Name", dt, ParameterType.Input, typeof(DateTime));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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
        var memb = new ContractMember("Name", val, ParameterType.Input, typeof(Guid));

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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

        var prop = GetType().GetProperty(nameof(RequiredString))!;

        var memb = new ContractMember(prop.Name, null, ParameterType.Input, prop);

        // Act
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            memb.ToDataParameter(cmdMock.Object, null);
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

        var prop = GetType().GetProperty(nameof(RequiredString))!;

        var memb = new ContractMember(prop.Name, "value", ParameterType.Input, prop);

        // Act
        var res = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual("value", res.Value);
    }

    [TestMethod]
    public void ToDataParameter__Required_primitive_parameter_default__OK()
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var prop = GetType().GetProperty(nameof(RequiredNumber))!;

        var memb = new ContractMember(prop.Name, 0, ParameterType.Input, prop);

        // Act
        var res = memb.ToDataParameter(cmdMock.Object, null);

        // Assert
        cmdMock.Verify();
        dbpMock.Verify();
        Assert.AreEqual(0, (int)res.Value!);
    }

    [TestMethod]
    public void ToDataParameter__Encrypts_secure_value()
    {
        // Arrange
        getDbMocks(out var cmdMock, out var dbpMock);

        var prop = GetType().GetProperty(nameof(SecureDataProperty))!;

        var memb = new ContractMember("Name", new byte[] { 0 }, ParameterType.Input, prop);

        // Act
        var dbp = memb.ToDataParameter(cmdMock.Object, null);

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
        public override object? FromDatasource(Type type, object? data, object? context)
        {
            return "FromDatasource_" + data;
        }

        public override object? ToDatasource(object? data, object? context)
        {
            return "ToDatasource_" + data;
        }
    }

    [TestTransphorm]
    public string TransphormedStringProperty { get; set; } = string.Empty;

    class TestSecureAttribute : AbstractSecureValueAttribute
    {
        public override byte[] Decrypt(byte[]? value, object? context)
        {
            return new byte[] { 1 };
        }

        public override byte[] Encrypt(object? value, object? context)
        {
            return new byte[] { 2 };
        }
    }

    [TestSecure]
    public byte[] SecureDataProperty { get; set; } = Array.Empty<byte>();

    [TestMethod]
    public void FromDatasource__DBNull_is_null()
    {
        var memb = ContractMember.Out<string>();

        memb.FromDatasource(DBNull.Value, null);

        Assert.IsNull(memb.Value);
    }

    [TestMethod]
    public void FromDatasource__Type_changed_to_T()
    {
        var memb = ContractMember.Out<int>();

        memb.FromDatasource("1234", null);

        Assert.AreEqual(1234, memb.Value);
        Assert.IsTrue(memb.HasChanged);
    }

    [TestMethod]
    public void FromDatasource__Transphorms_value()
    {
        var prop = GetType().GetProperty(nameof(TransphormedStringProperty))!;

        var memb = new ContractMember("name", null, ParameterType.Output, prop);

        memb.FromDatasource("Value", null);

        Assert.AreEqual("FromDatasource_Value", memb.Value);
    }

    [TestMethod]
    public void FromDatasource__Decrypts_value()
    {
        var prop = GetType().GetProperty(nameof(SecureDataProperty))!;

        var memb = new ContractMember("name", null, ParameterType.Output, prop);

        memb.FromDatasource(new byte[] { 0 }, null);

        CollectionAssert.AreEqual(new byte[] { 1 }, (byte[])memb.Value!);
    }

    #endregion FromDatasource

    [TestMethod]
    [DataRow(typeof(int), "12345", 12345)]
    [DataRow(typeof(int?), "12345", 12345)]
    [DataRow(typeof(double), "12.34", 12.34)]
    public void SetValue__Converts_value_to_type_T(Type valueType, string value, object exp)
    {
        var memb = new ContractMember(null, default, ParameterType.Input, valueType);

        memb.SetValue(value);

        Assert.AreEqual(exp, memb.Value);
    }

    [TestMethod]
    [DataRow(nameof(IntProperty), "12345", 12345)]
    [DataRow(nameof(NullableIntProperty), "12345", 12345)]
    public void SetValue__Converts_value_to_property_type(string propertyName, string value, object exp)
    {
        var prop = GetType().GetProperty(propertyName)!;

        var memb = new ContractMember(propertyName, null, ParameterType.Input, prop);

        memb.SetValue(value);
        _ = ((PropertyInfo)memb.SourceMember!).GetValue(this);

        Assert.AreEqual(exp, memb.Value);
    }
}