using IFY.Phorm.Execution;
using IFY.Phorm.Tests;
using System.Reflection;

namespace IFY.Phorm.Data.Tests;

[TestClass]
public class GenSpecTests
{
    public TestContext TestContext { get; set; }

    // Gen
    abstract class AbstractBaseGenType
    {
    }
    class BaseGenType : AbstractBaseGenType
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; }
    }

    // Specs
    [PhormSpecOf(nameof(TypeId), 1)]
    class SpecType1 : BaseGenType
    {
        public int IntSpecProperty { get; set; }
    }

    [PhormSpecOf(nameof(TypeId), 2)]
    class SpecType2 : BaseGenType
    {
        public string StringSpecProperty { get; set; } = string.Empty;
    }

    class TypeWithoutAttribute : BaseGenType
    {
    }

    [PhormSpecOf("BadProperty", 2)]
    class TypeWithBadAttribute : BaseGenType
    {
    }

    private static IPhormContractRunner buildRunner()
    {
        var conn = new TestPhormConnection()
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Id"] = 1,
                    ["Name"] = "Row1",
                    ["TypeId"] = 1, // Int
                    ["IntSpecProperty"] = 12345
                },
                new()
                {
                    ["Id"] = 2,
                    ["Name"] = "Row2",
                    ["TypeId"] = 2, // String
                    ["StringSpecProperty"] = "Value"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        return new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, null, null);
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public async Task GetAsync__GenSpec__Shapes_records_by_selector()
    {
        // Arrange
        var runner = buildRunner();

        // Act
        var result = await runner.GetAsync<GenSpec<BaseGenType, SpecType1, SpecType2>>(TestContext.CancellationToken);

        var all = result!.All();
        var spec1 = result.OfType<SpecType1>().ToArray();
        var spec2 = result.OfType<SpecType2>().ToArray();

        // Assert
        Assert.HasCount(2, all);
        Assert.AreEqual(12345, spec1.Single().IntSpecProperty);
        Assert.AreEqual("Value", spec2.Single().StringSpecProperty);
    }

    [TestMethod]
    public async Task GetAsync__GenSpec__Unmapped_type_returned_as_nonabstract_base()
    {
        // Arrange
        var runner = buildRunner();

        // Act
        var result = await runner.GetAsync<GenSpec<BaseGenType, SpecType1>>(TestContext.CancellationToken);

        var all = result!.All();
        var asBase = all.Where(r => r.GetType() == typeof(BaseGenType)).ToArray();
        var spec1 = result.OfType<SpecType1>().ToArray();
        var spec2 = result.OfType<SpecType2>().ToArray();

        // Assert
        Assert.HasCount(2, all);
        Assert.HasCount(1, asBase);
        Assert.HasCount(1, spec1);
        Assert.IsEmpty(spec2);
    }

    [TestMethod]
    public async Task GetAsync__GenSpec__Unmapped_type_ignored_for_abstract_base()
    {
        // Arrange
        var runner = buildRunner();

        // Act
        var result = await runner.GetAsync<GenSpec<AbstractBaseGenType, SpecType1>>(TestContext.CancellationToken);

        var all = result!.All();
        var spec1 = result.OfType<SpecType1>().ToArray();
        var spec2 = result.OfType<SpecType2>().ToArray();

        // Assert
        Assert.HasCount(1, all);
        Assert.HasCount(1, spec1);
        Assert.IsEmpty(spec2);
    }

    [TestMethod]
    public async Task GetAsync__GenSpec__Type_without_attribute__Fail()
    {
        // Arrange
        var runner = buildRunner();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<TargetInvocationException>
            (async () => await runner.GetAsync<GenSpec<BaseGenType, TypeWithoutAttribute>>(TestContext.CancellationToken));

        // Assert
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
        Assert.AreEqual("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + typeof(TypeWithoutAttribute).FullName, ex.InnerException.Message);
    }

    [TestMethod]
    public async Task GetAsync__GenSpec__Type_referencing_bad_property__Fail()
    {
        // Arrange
        var runner = buildRunner();

        // Act
        var ex = await Assert.ThrowsExactlyAsync<TargetInvocationException>
            (async () => await runner.GetAsync<GenSpec<BaseGenType, TypeWithBadAttribute>>(TestContext.CancellationToken));

        // Assert
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
        Assert.AreEqual("Invalid GenSpec usage. Provided type was not decorated with a PhormSpecOfAttribute referencing a valid property: " + typeof(TypeWithBadAttribute).FullName, ex.InnerException.Message);
    }
}
