using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace IFY.Phorm.Tests;

[TestClass]
public class ExtensionsTests
{
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

    #region GetBytes

    [TestMethod]
    public void GetBytes__Null__Empty()
    {
        // Act
        var res = ((object?)null).GetBytes();

        // Assert
        Assert.AreEqual(0, res.Length);
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
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            _ = new ExtensionsTests().GetBytes();
        });
    }

    #endregion GetBytes

    #region GetReferencedObjectProperties

    class TestObject
    {
        public int Numeric { get; set; }
        public string String { get; set; } = null!;
        public bool Boolean { get; set; }
        public byte[] Array { get; set; } = null!;
        public double? Nullable { get; set; }
        //public TestObject Child { get; set; }
    }

    [TestMethod]
    public void GetReferencedObjectProperties__Can_handle_all_individual_expressions()
    {
        static void exprRefsProperty(Expression<Func<TestObject, bool>> fn, string prop)
        {
            _ = fn.Compile(); // Must compile
            var props = fn.Body.GetExpressionParameterProperties(typeof(TestObject));
            Assert.AreEqual(prop, props.Single().Name, prop);
        }

        // Numeric
        exprRefsProperty(o => o.Numeric == 0, nameof(TestObject.Numeric));
        exprRefsProperty(o => (double)o.Numeric == 0, nameof(TestObject.Numeric));
        exprRefsProperty(o => o.Numeric != 0, nameof(TestObject.Numeric));
        exprRefsProperty(o => o.Numeric < 0, nameof(TestObject.Numeric));
        exprRefsProperty(o => o.Numeric > 0, nameof(TestObject.Numeric));

        // String
        exprRefsProperty(o => o.String == string.Empty, nameof(TestObject.String));
        exprRefsProperty(o => o.String != null, nameof(TestObject.String));
        exprRefsProperty(o => o.String.Length > 0, nameof(TestObject.String));
        exprRefsProperty(o => o.String.Contains(""), nameof(TestObject.String));
        exprRefsProperty(o => o.String.ToLower() == string.Empty, nameof(TestObject.String));

        var re = new Regex("");
        exprRefsProperty(o => re.IsMatch(o.String), nameof(TestObject.String));

        // Boolean
        exprRefsProperty(o => o.Boolean, nameof(TestObject.Boolean));
        exprRefsProperty(o => !o.Boolean, nameof(TestObject.Boolean));
#pragma warning disable IDE0075 // Simplify conditional expression
        exprRefsProperty(o => o.Boolean ? false : true, nameof(TestObject.Boolean));
#pragma warning restore IDE0075 // Simplify conditional expression

        // Array
        exprRefsProperty(o => o.Array.IsFixedSize, nameof(TestObject.Array));
        exprRefsProperty(o => o.Array.Length > 0, nameof(TestObject.Array));
        exprRefsProperty(o => o.Array.GetLength(0) > 0, nameof(TestObject.Array));
        exprRefsProperty(o => o.Array[0] != 0, nameof(TestObject.Array));
        exprRefsProperty(o => o.Array.Any(), nameof(TestObject.Array));

        // Nullable
        exprRefsProperty(o => o.Nullable == null, nameof(TestObject.Nullable));
        exprRefsProperty(o => o.Nullable.HasValue, nameof(TestObject.Nullable));
        exprRefsProperty(o => !o.Nullable.HasValue, nameof(TestObject.Nullable));
        exprRefsProperty(o => o.Nullable!.Value > 0, nameof(TestObject.Nullable));
    }

    [TestMethod]
    public void GetReferencedObjectProperties__Can_handle_complex_expressions()
    {
        static void exprRefsProperty(string name, Expression<Func<TestObject, bool>> fn, params string[] expProps)
        {
            _ = fn.Compile(); // Must compile
            var props = fn.Body.GetExpressionParameterProperties(typeof(TestObject));
            CollectionAssert.AreEquivalent(expProps, props.Select(p => p.Name).ToArray(), name);
        }

        exprRefsProperty("Multiple properties", o => (o.Numeric > 0 && o.String != null && o.Boolean && o.Array.Length > 0) || o.Nullable.HasValue, nameof(TestObject.Numeric), nameof(TestObject.String), nameof(TestObject.Boolean), nameof(TestObject.Array), nameof(TestObject.Nullable));

        exprRefsProperty("Multiple uses returned once", o => o.Numeric == 0 && (double)o.Numeric == 0 && o.Numeric != 0 && o.Numeric < 0 && o.Numeric > 0, nameof(TestObject.Numeric));
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
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            _ = Extensions.FromBytes(Array.Empty<byte>(), typeof(ExtensionsTests));
        });
    }

    #endregion FromBytes
}