using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Execution;
using IFY.Phorm.Transformation;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace IFY.Phorm.Tests.Execution;

[TestClass]
public class PhormContractRunnerTests_Records
{
    public TestContext TestContext { get; set; }

    record Record_FullPrimary(string Value, int Number);

    private async Task<T> getResult<T>(List<Dictionary<string, object>> data)
        where T : class
    {
        var conn = new TestPhormConnection(Guid.NewGuid().ToString())
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader { Data = data });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);
        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 }, null);

        return (await runner.GetAsync<T>(TestContext.CancellationToken))!;
    }

    [TestMethod]
    public async Task GetAsync__One__Support_record_with_primary_constructor()
    {
        // Act
        var res = await getResult<Record_FullPrimary>([new() { ["Value"] = "value1", ["Number"] = 10 }]);

        // Assert
        Assert.AreEqual("value1", res.Value);
        Assert.AreEqual(10, res.Number);
    }

    [TestMethod]
    public async Task GetAsync__Many__Support_record_with_primary_constructor()
    {
        // Act
        var res = await getResult<Record_FullPrimary[]>([
            new() { ["Value"] = "value1", ["Number"] = 10 },
            new() { ["Value"] = "value2", ["Number"] = 20 },
        ]);

        // Assert
        Assert.HasCount(2, res);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual(10, res[0].Number);
        Assert.AreEqual("value2", res[1].Value);
        Assert.AreEqual(20, res[1].Number);
    }

    record Record_PartialPrimary(string Value)
    {
        public int Number { get; init; }
    }

    [TestMethod]
    public async Task GetAsync__One__Support_record_with_partial_primary_constructor()
    {
        // Act
        var res = await getResult<Record_PartialPrimary>([new() { ["Value"] = "value1", ["Number"] = 10 }]);

        // Assert
        Assert.AreEqual("value1", res.Value);
        Assert.AreEqual(10, res.Number);
    }

    [TestMethod]
    public async Task GetAsync__Many__Support_record_with_partial_primary_constructor()
    {
        // Act
        var res = await getResult<Record_PartialPrimary[]>([
            new() { ["Value"] = "value1", ["Number"] = 10 },
            new() { ["Value"] = "value2", ["Number"] = 20 },
        ]);

        // Assert
        Assert.HasCount(2, res);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual(10, res[0].Number);
        Assert.AreEqual("value2", res[1].Value);
        Assert.AreEqual(20, res[1].Number);
    }

    record Record_NoConstructor
    {
        public required string Value { get; init; }
        public int Number { get; init; }
    }

    [TestMethod]
    public async Task GetAsync__One__Support_record_without_constructor()
    {
        // Act
        var res = await getResult<Record_NoConstructor>([new() { ["Value"] = "value1", ["Number"] = 10 }]);

        // Assert
        Assert.AreEqual("value1", res.Value);
        Assert.AreEqual(10, res.Number);
    }

    [TestMethod]
    public async Task GetAsync__Many__Support_record_without_constructor()
    {
        // Act
        var res = await getResult<Record_NoConstructor[]>([
            new() { ["Value"] = "value1", ["Number"] = 10 },
            new() { ["Value"] = "value2", ["Number"] = 20 },
        ]);

        // Assert
        Assert.HasCount(2, res);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual(10, res[0].Number);
        Assert.AreEqual("value2", res[1].Value);
        Assert.AreEqual(20, res[1].Number);
    }

    record Record_ManyConstructors(string Id)
    {
        public int ConstructorUsed { get; private set; } = 1;
        public string Name { get; set; } = string.Empty;

        public Record_ManyConstructors(string id, string name) : this(id)
        {
            Name = name;
            ConstructorUsed = 2;
        }
    }

    [TestMethod]
    public async Task GetAsync__One__Attempts_to_use_primary_constructor()
    {
        // Act
        var res = await getResult<Record_ManyConstructors>([new() { ["Id"] = "id1", ["Name"] = "name1" }]);

        // Assert
        Assert.AreEqual("id1", res.Id);
        Assert.AreEqual("name1", res.Name);
        Assert.AreEqual(1, res.ConstructorUsed);
    }

    record Record_ConstructorSecureValue(
        string Key,
        [property: SecureValue("class")]
        string Value
    );

    [TestMethod]
    public async Task GetAsync__Many__Record_constructor_does_not_support_secure_values()
    {
        // Arrange
        var mocks = new MockRepository(MockBehavior.Strict);

        var encdata1 = Guid.NewGuid().ToString().GetBytes();
        var encdata2 = Guid.NewGuid().ToString().GetBytes();

        GlobalSettings.EncryptionProvider = null;

        // Act
        var res = await getResult<Record_ConstructorSecureValue[]>([
            new() { ["Key"] = "key1", ["Value"] = encdata1 },
            new() { ["Key"] = "key2", ["Value"] = encdata2 },
        ]);

        // Assert
        mocks.Verify();
        Assert.HasCount(2, res);
        Assert.AreEqual("key1", res[0].Key);
        Assert.AreEqual(encdata1.FromBytes<string>(), res[0].Value);
        Assert.AreEqual("key2", res[1].Key);
        Assert.AreEqual(encdata2.FromBytes<string>(), res[1].Value);
    }

    record Record_PropertySecureValue(string Key)
    {
        [SecureValue("class")]
        public required string Value { get; set; }
    }

    [TestMethod]
    public async Task GetAsync__Many__Records_support_secure_values()
    {
        // Arrange
        var mocks = new MockRepository(MockBehavior.Strict);

        var data1 = "value1";
        var encdata1 = Guid.NewGuid().ToString().GetBytes();
        var data2 = "value2";
        var encdata2 = Guid.NewGuid().ToString().GetBytes();

        var decrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        decrMock.SetupProperty(m => m.Authenticator);
        decrMock.Setup(m => m.Decrypt(encdata1))
            .Returns(data1.GetBytes()).Verifiable();
        decrMock.Setup(m => m.Decrypt(encdata2))
            .Returns(data2.GetBytes()).Verifiable();

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetDecryptor("class", encdata1))
            .Returns(() => decrMock.Object);
        provMock.Setup(m => m.GetDecryptor("class", encdata2))
            .Returns(() => decrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        // Act
        var res = await getResult<Record_PropertySecureValue[]>([
            new() { ["Key"] = "key1", ["Value"] = encdata1 },
            new() { ["Key"] = "key2", ["Value"] = encdata2 },
        ]);

        // Assert
        mocks.Verify();
        Assert.HasCount(2, res);
        Assert.AreEqual("key1", res[0].Key);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual("key2", res[1].Key);
        Assert.AreEqual("value2", res[1].Value);
    }

    class TestTransformerAttribute : AbstractTransphormAttribute
    {
        public override object? FromDatasource(Type type, object? data, object? context)
            => "Transformed_" + (string?)data;
        [ExcludeFromCodeCoverage]
        public override object? ToDatasource(object? data, object? context)
            => throw new NotImplementedException();
    }

    record Record_ConstructorTransphormed(
        string Key,
        [property: TestTransformer]
        string Value
    );

    [TestMethod]
    public async Task GetAsync__Many__Record_constructor_does_not_support_transphormed_values()
    {
        // Arrange
        var mocks = new MockRepository(MockBehavior.Strict);

        // Act
        var res = await getResult<Record_ConstructorTransphormed[]>([
            new() { ["Key"] = "key1", ["Value"] = "value1" },
            new() { ["Key"] = "key2", ["Value"] = "value2" },
        ]);

        // Assert
        mocks.Verify();
        Assert.HasCount(2, res);
        Assert.AreEqual("key1", res[0].Key);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual("key2", res[1].Key);
        Assert.AreEqual("value2", res[1].Value);
    }

    record Record_PropertyTransphormed(string Key)
    {
        [TestTransformer]
        public required string Value { get; init; }
    }

    [TestMethod]
    public async Task GetAsync__Many__Records_transphormed_properties()
    {
        // Arrange
        var mocks = new MockRepository(MockBehavior.Strict);

        // Act
        var res = await getResult<Record_PropertyTransphormed[]>([
            new() { ["Key"] = "key1", ["Value"] = "value1" },
            new() { ["Key"] = "key2", ["Value"] = "value2" },
        ]);

        // Assert
        mocks.Verify();
        Assert.HasCount(2, res);
        Assert.AreEqual("key1", res[0].Key);
        Assert.AreEqual("Transformed_value1", res[0].Value);
        Assert.AreEqual("key2", res[1].Key);
        Assert.AreEqual("Transformed_value2", res[1].Value);
    }
}
