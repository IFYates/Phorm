using Moq;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace IFY.Phorm.Tests;

[TestClass]
public class ExtensionsTests
{
    #region ChangeType

    [TestMethod]
    public void ChangeType__Subtype__No_change()
    {
        // Arrange
        var str = "value";

        // Act
        var res = str.ChangeType(typeof(object));

        // Assert
        Assert.AreSame(str, (string)res);
    }

    [TestMethod]
    public void ChangeType__Null()
    {
        // Act
        var res = ((object?)null).ChangeType(typeof(object));

        // Assert
        Assert.IsNull(res);
    }

    public enum MyEnum
    {
        [EnumMember]
        Fail = 0,
        Pass
    }

    [TestMethod]
    [DataRow(MyEnum.Fail, 0), DataRow(MyEnum.Pass, 1)]
    public void ChangeType__From_Enum(MyEnum value, int exp)
    {
        // Act
        var res = value.ChangeType(typeof(int));

        // Assert
        Assert.AreEqual(exp, res);
    }

    [TestMethod]
    [DataRow((byte)1), DataRow((short)1), DataRow(1), DataRow(1L)]
    [DataRow((sbyte)1), DataRow((ushort)1), DataRow((uint)1), DataRow((ulong)1)]
    [DataRow("Pass"), DataRow("pass"), DataRow("PASS")]
    public void ChangeType__To_Enum(object value)
    {
        // Act
        var res = (MyEnum)value.ChangeType(typeof(MyEnum));

        // Assert
        Assert.AreEqual(MyEnum.Pass, res);
    }

    #endregion ChangeType

    #region GetBytes

    [TestMethod]
    public void GetBytes__Null__Empty()
    {
        // Act
        var res = ((object?)null).GetBytes();

        // Assert
        Assert.IsEmpty(res);
    }

    [TestMethod]
#if NET6_0_OR_GREATER
    [DataRow(new byte[] { 110, 1, 0, 0 }, "0001-01-01", typeof(DateOnly))]
    [DataRow(new byte[] { 83, 49, 11, 0 }, "2004-02-29", typeof(DateOnly))]
    [DataRow(new byte[] { 133, 50, 11, 0 }, "2004-12-31", typeof(DateOnly))]
    [DataRow(new byte[] { 222, 216, 55, 0 }, "9999-12-31", typeof(DateOnly))]
#endif
    [DataRow(new byte[] { 7, 23, 69, 137, 156, 205, 217, 8 }, "2022-01-02 03:04:05.1234567", typeof(DateTime))]
    [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0 }, 12.34, typeof(decimal))]
    [DataRow(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4 }, typeof(byte[]))]
    [DataRow(new byte[] { 123 }, 123, typeof(byte))]
    [DataRow(new byte[] { 123, 0 }, 123, typeof(char))]
    [DataRow(new byte[] { 174, 71, 225, 122, 20, 174, 40, 64 }, 12.34, typeof(double))]
    [DataRow(new byte[] { 164, 112, 69, 65 }, 12.34, typeof(float))]
    [DataRow(new byte[] { 210, 4, 0, 0 }, 1234, typeof(int))]
    [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0 }, 1234, typeof(long))]
    [DataRow(new byte[] { 210, 4 }, 1234, typeof(short))]
    [DataRow(new byte[] { 49, 50, 51, 52 }, "1234", typeof(string))]
    public void GetBytes__Supports_basic_types(byte[] exp, object data, Type type)
    {
        // Arrange
        if (data.GetType() != type)
        {
            data = data.ChangeType(type);
        }

        // Act
        var res = data.GetBytes();

        // Assert
        CollectionAssert.AreEqual(exp, res);
    }

    [TestMethod]
    public void GetBytes__Supports_Guid()
    {
        // Arrange
        var exp = new byte[]
        {
            131, 190, 113, 220, 122, 94, 160, 76, 178, 33, 70, 205, 145, 251, 255, 152
        };

        var data = Guid.Parse("dc71be83-5e7a-4ca0-b221-46cd91fbff98");

        // Act
        var res = data.GetBytes();

        // Assert
        CollectionAssert.AreEqual(exp, res);
    }

    [TestMethod]
    public void GetBytes__Unsupported_type__Exception()
    {
        // Act
        Assert.ThrowsExactly<InvalidCastException>
            (() => new ExtensionsTests().GetBytes());
    }

    #endregion GetBytes

    #region GetReferencedObjectProperties

    [ExcludeFromCodeCoverage]
    class TestObject
    {
        public int Numeric { get; set; }
        public string String { get; set; } = null!;
        public bool Boolean { get; set; }
        public byte[] Array { get; set; } = null!;
        public double? Nullable { get; set; }
        //public TestObject Child { get; set; }
    }

    public const string OtherObjectConst = null!;
    public string OtherObjectInstanceField = null!;
    public static string OtherObjectStaticField = null!;
    [ExcludeFromCodeCoverage]
    public static string OtherObjectStaticProperty { get; } = null!;
    [ExcludeFromCodeCoverage]
    public static bool OtherObjectStaticMethod(string str) => true;
    [ExcludeFromCodeCoverage]
    public bool OtherObjectInstanceMethod(string str) => true;

    [TestMethod]
    public void GetReferencedObjectProperties__Expression_does_not_require_property_references()
    {
        Expression<Func<TestObject, bool>> expr = (o) => true;

        _ = expr.Compile(); // Must compile
        var props = expr.Body.GetExpressionParameterProperties(typeof(TestObject));

        Assert.IsEmpty(props);
    }

    [TestMethod]
    public void GetReferencedObjectProperties__Can_handle_all_individual_expressions()
    {
        static void assertExprRefProp(Expression<Func<TestObject, bool>> fn, string prop)
        {
            _ = fn.Compile(); // Must compile
            var props = fn.Body.GetExpressionParameterProperties(typeof(TestObject));
            Assert.AreEqual(prop, props.Single().Name, prop);
        }

        // Numeric
        assertExprRefProp(o => o.Numeric == 0, nameof(TestObject.Numeric));
        assertExprRefProp(o => (double)o.Numeric == 0, nameof(TestObject.Numeric));
        assertExprRefProp(o => o.Numeric != 0, nameof(TestObject.Numeric));
        assertExprRefProp(o => o.Numeric < 0, nameof(TestObject.Numeric));
        assertExprRefProp(o => o.Numeric > 0, nameof(TestObject.Numeric));

        // String
        assertExprRefProp(o => o.String == string.Empty, nameof(TestObject.String));
        assertExprRefProp(o => o.String != null, nameof(TestObject.String));
        assertExprRefProp(o => o.String.Length > 0, nameof(TestObject.String));
        assertExprRefProp(o => o.String.Contains(""), nameof(TestObject.String));
        assertExprRefProp(o => o.String.ToLower() == string.Empty, nameof(TestObject.String));

        var re = new Regex("");
        assertExprRefProp(o => re.IsMatch(o.String), nameof(TestObject.String));

        // Boolean
        assertExprRefProp(o => o.Boolean, nameof(TestObject.Boolean));
        assertExprRefProp(o => !o.Boolean, nameof(TestObject.Boolean));
#pragma warning disable IDE0075 // Simplify conditional expression
        assertExprRefProp(o => o.Boolean ? false : true, nameof(TestObject.Boolean));
#pragma warning restore IDE0075 // Simplify conditional expression

        // Array
        assertExprRefProp(o => o.Array.IsFixedSize, nameof(TestObject.Array));
        assertExprRefProp(o => o.Array.Length > 0, nameof(TestObject.Array));
        assertExprRefProp(o => o.Array.GetLength(0) > 0, nameof(TestObject.Array));
        assertExprRefProp(o => o.Array[0] != 0, nameof(TestObject.Array));
        assertExprRefProp(o => o.Array.Any(), nameof(TestObject.Array));

        // Nullable
        assertExprRefProp(o => o.Nullable == null, nameof(TestObject.Nullable));
        assertExprRefProp(o => o.Nullable.HasValue, nameof(TestObject.Nullable));
        assertExprRefProp(o => !o.Nullable.HasValue, nameof(TestObject.Nullable));
        assertExprRefProp(o => o.Nullable!.Value > 0, nameof(TestObject.Nullable));
    }

    [TestMethod]
    public void GetReferencedObjectProperties__Can_handle_complex_expressions()
    {
        static void assertExprRefProps(string name, Expression<Func<TestObject, bool>> fn, params string[] expProps)
        {
            _ = fn.Compile(); // Must compile
            var props = fn.Body.GetExpressionParameterProperties(typeof(TestObject));
            CollectionAssert.AreEquivalent(expProps, props.Select(p => p.Name).ToArray(), name);
        }

        assertExprRefProps("Multiple properties", o => (o.Numeric > 0 && o.String != null && o.Boolean && o.Array.Length > 0) || o.Nullable.HasValue, nameof(TestObject.Numeric), nameof(TestObject.String), nameof(TestObject.Boolean), nameof(TestObject.Array), nameof(TestObject.Nullable));

        assertExprRefProps("Multiple uses returned once", o => o.Numeric == 0 && (double)o.Numeric == 0 && o.Numeric != 0 && o.Numeric < 0 && o.Numeric > 0, nameof(TestObject.Numeric));

        assertExprRefProps("Accepts external static members", o => o.String == (OtherObjectConst + OtherObjectStaticField + OtherObjectStaticProperty), nameof(TestObject.String));
        assertExprRefProps("Accepts static method", o => OtherObjectStaticMethod(o.String), nameof(TestObject.String));

        assertExprRefProps("Accepts external instance members", o => o.String == OtherObjectInstanceField, nameof(TestObject.String));
        assertExprRefProps("Accepts external instance method", o => OtherObjectInstanceMethod(o.String), nameof(TestObject.String));
    }

    [TestMethod]
    public void GetReferencedObjectProperties__Fails_for_unrecognised_expression()
    {
        // Unknown expression type
        var exprMock = new Mock<Expression>();
        Assert.ThrowsExactly<NotImplementedException>
            (() => exprMock.Object.GetExpressionParameterProperties(typeof(TestObject)));
    }

    #endregion GetReferencedObjectProperties

    #region FromBytes

    [TestMethod]
    public void FromBytes__Null__Null()
    {
        // Act
        var res = Extensions.FromBytes<string>(null);

        // Assert
        Assert.IsNull(res);
    }

    [TestMethod]
#if NET6_0_OR_GREATER
    [DataRow(new byte[] { 110, 1, 0, 0 }, "0001-01-01", typeof(DateOnly))]
    [DataRow(new byte[] { 83, 49, 11, 0 }, "2004-02-29", typeof(DateOnly))]
    [DataRow(new byte[] { 133, 50, 11, 0 }, "2004-12-31", typeof(DateOnly))]
    [DataRow(new byte[] { 222, 216, 55, 0 }, "9999-12-31", typeof(DateOnly))]
#endif
    [DataRow(new byte[] { 7, 23, 69, 137, 156, 205, 217, 8 }, "2022-01-02 03:04:05.1234567", typeof(DateTime))]
    [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0 }, 12.34, typeof(decimal))]
    [DataRow(new byte[] { 1, 2, 3, 4 }, new byte[] { 1, 2, 3, 4 }, typeof(byte[]))]
    [DataRow(new byte[] { 123 }, 123, typeof(byte))]
    [DataRow(new byte[] { 123, 0 }, 123, typeof(char))]
    [DataRow(new byte[] { 174, 71, 225, 122, 20, 174, 40, 64 }, 12.34, typeof(double))]
    [DataRow(new byte[] { 164, 112, 69, 65 }, 12.34, typeof(float))]
    [DataRow(new byte[] { 210, 4, 0, 0 }, 1234, typeof(int))]
    [DataRow(new byte[] { 210, 4, 0, 0, 0, 0, 0, 0 }, 1234, typeof(long))]
    [DataRow(new byte[] { 210, 4 }, 1234, typeof(short))]
    [DataRow(new byte[] { 49, 50, 51, 52 }, "1234", typeof(string))]
    public void FromBytes__Supports_basic_types(byte[] data, object exp, Type type)
    {
        // Act
        if (exp.GetType() != type)
        {
            exp = exp.ChangeType(type);
        }

        var res = Extensions.FromBytes(data, type);

        // Assert
        if (type.IsArray)
        {
            CollectionAssert.AreEqual((ICollection)exp, (ICollection?)res);
        }
        else
        {
            Assert.AreEqual(exp, res);
        }
    }

    [TestMethod]
    public void FromBytes__Supports_Guid()
    {
        // Arrange
        var data = new byte[]
        {
            131, 190, 113, 220, 122, 94, 160, 76, 178, 33, 70, 205, 145, 251, 255, 152
        };

        // Act
        var res = data.FromBytes<Guid>();

        // Assert
        Assert.AreEqual("dc71be83-5e7a-4ca0-b221-46cd91fbff98", res.ToString());
    }

    [TestMethod]
    public void FromBytes__Unsupported_type__Exception()
    {
        // Act
        Assert.ThrowsExactly<InvalidCastException>
            (() => Extensions.FromBytes([], typeof(ExtensionsTests)));
    }

    #endregion FromBytes
}