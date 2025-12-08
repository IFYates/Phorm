using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Tests;
using IFY.Phorm.Transformation;
using Moq;
using System.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests_Call
{
    public TestContext TestContext { get; set; }

    public class TestContract : IMemberTestContract
    {
        public int Arg { get; set; }
        [IgnoreDataMember]
        public int Arg2 { get; set; }
        [IgnoreDataMember]
        public string Arg3 { get; set; } = string.Empty;
        [IgnoreDataMember]
        public ContractMember Arg4 { get; set; } = new ContractMember("InvalidRename", null, ParameterType.Output, typeof(string));
    }

    public class TestContractWithTransphormer : IPhormContract
    {
        [TestTransphorm]
        public int Arg { get; set; }
    }

    public class TestContract2 : IPhormContract
    {
        [ContractMember(DisableOutput = true)]
        public string Input { get; set; } = string.Empty;
        [ContractMember(DisableInput = true)]
        public string Output { get; set; } = string.Empty;
        public string InputOutput { get; set; } = string.Empty;
        [ContractMember(DisableInput = true, DisableOutput = true)]
        public string Ignored { get; set; } = string.Empty;
    }

    [PhormContract(Name = "IAmRenamedContract", Namespace = "otherSchema")]
    public interface IMemberTestContract : IPhormContract
    {
        [DataMember(Name = "RenamedArg")]
        int Arg { get; }
        int Arg2 { set; } // Out
        [DataMember(IsRequired = true)]
        string Arg3 { get; }
        ContractMember Arg4 { get; }
    }

    public interface ISecureTestContract : IPhormContract
    {
        [IgnoreDataMember]
        int Arg { get; }
        [DataMember(Name = "SecureArg")]
        [SecureValue("class", nameof(Arg))]
        string? Arg3 { get; set; }
    }

    public class TestTransphormAttribute : AbstractTransphormAttribute
    {
        public static object? FromDatasourceReturnValue = null;
        public static object? ToDatasourceReturnValue = null;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override object? FromDatasource(Type type, object? data, object? context)
            => FromDatasourceReturnValue;
        public override object? ToDatasource(object? data, object? context)
            => ToDatasourceReturnValue;
    }

    [TestInitialize]
    public void Init()
    {
        AbstractPhormSession.ResetConnectionPool();
    }

    [TestMethod]
    public async Task Call_by_anon_object()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure, new { Arg = 1 }, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_CallTest]", cmd.CommandText);

        var pars = cmd.Parameters.AsParameters();
        Assert.HasCount(2, pars);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
    }

    [TestMethod]
    public async Task Call_by_contract()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, null, DbObjectType.StoredProcedure, new TestContract { Arg = 1 }, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);

        var pars = cmd.Parameters.AsParameters();
        Assert.HasCount(2, pars);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.InputOutput, pars[0].Direction);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
    }

    [TestMethod]
    [DynamicData(nameof(IgnoreDataMemberAttributeProvider))]
    public async Task Call__Transphormer_removes_parameter(object value)
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        TestTransphormAttribute.ToDatasourceReturnValue = value;

        var runner = new PhormContractRunner<TestContractWithTransphormer>(phorm, null, DbObjectType.StoredProcedure, new TestContractWithTransphormer { Arg = 1 }, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContractWithTransphormer]", cmd.CommandText);

        var pars = cmd.Parameters.AsParameters();
        Assert.HasCount(1, pars);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[0].Direction);
    }
    private static IEnumerable<object[]> IgnoreDataMemberAttributeProvider()
    {
        yield return new object[] { typeof(IgnoreDataMemberAttribute) };
        yield return new object[] { new IgnoreDataMemberAttribute() };
    }

    [TestMethod]
    public async Task Call__Contract_and_arg_rename()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, new TestContract { Arg = 1 }, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[otherSchema].[usp_IAmRenamedContract]", cmd.CommandText);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual("@RenamedArg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
    }

    [TestMethod]
    public async Task Call__Concrete_contract_with_input_and_output()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract2>(phorm, null, DbObjectType.Default, new TestContract2(), null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);

        var pars = cmd.Parameters.AsParameters();
        Assert.HasCount(4, pars); // + return
        Assert.AreEqual("@Input", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
        Assert.AreEqual("@Output", pars[1].ParameterName);
        Assert.AreEqual(ParameterDirection.Output, pars[1].Direction);
        Assert.AreEqual("@InputOutput", pars[2].ParameterName);
        Assert.AreEqual(ParameterDirection.InputOutput, pars[2].Direction);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[3].Direction);
    }

    [TestMethod]
    public async Task Call__Out_arg()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var args = new TestContract { Arg = 1 };
        var cm = args.Arg4;

        var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, args, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual("@Arg2", pars[1].ParameterName);
        Assert.AreEqual(ParameterDirection.Output, pars[1].Direction);
        Assert.AreEqual("@Arg4", pars[3].ParameterName);
        Assert.AreEqual(ParameterDirection.Output, pars[3].Direction);
        Assert.AreSame(cm, args.Arg4);
    }

    [TestMethod]
    public async Task Call__Required_arg_null__Exception()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, new TestContract { Arg3 = null! }, null);

        // Act
        await Assert.ThrowsExactlyAsync<ArgumentNullException>
            (async () => await runner.CallAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Call__SecureValue_sent_encrypted_received_decrypted_by_authenticator()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var data = "secure_value".GetBytes();
        var encdata = Guid.NewGuid().ToString().GetBytes();

        var decrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        decrMock.SetupProperty(m => m.Authenticator);
        decrMock.Setup(m => m.Decrypt(encdata))
            .Returns(data);

        var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
        encrMock.SetupProperty(m => m.Authenticator);
        encrMock.Setup(m => m.Encrypt(data))
            .Returns(encdata);

        var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
        provMock.Setup(m => m.GetDecryptor("class", encdata))
            .Returns(() => decrMock.Object);
        provMock.Setup(m => m.GetEncryptor("class"))
            .Returns(() => encrMock.Object);
        GlobalSettings.EncryptionProvider = provMock.Object;

        var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

        var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default, dto, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);
        Assert.AreEqual("secure_value", dto.Arg3);
        CollectionAssert.AreEqual(100.GetBytes(), decrMock.Object.Authenticator);
    }

    [TestMethod]
    public async Task Call__Returns_result__Exception()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand(new TestDbDataReader
        {
            Data =
            [
                new()
                {
                    ["Value"] = "value1"
                }
            ]
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure, new { Arg = 1 }, null);

        // Act
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>
            (async () => await runner.CallAsync(TestContext.CancellationToken));

        // Assert
        Assert.AreEqual("Phorm non-result request returned a result.", ex.Message);
    }

    #region Console messages

    interface IConsoleLogContract : IPhormContract
    {
        int Arg { get; }
        ConsoleLogMember ConsoleLogs { get; }
    }

    class ConsoleLogContract : IConsoleLogContract
    {
        public int Arg { get; set; }

        public ConsoleLogMember ConsoleLogs { get; } = ContractMember.Console();
    }

    [TestMethod]
    public async Task Call__Contract_can_receive_console_messages()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
        };

        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

        var arg = new ConsoleLogContract { Arg = 1 };

        var runner = new PhormContractRunner<IConsoleLogContract>(phorm, null, DbObjectType.Default, arg, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);

        Assert.HasCount(3, arg.ConsoleLogs.Value);
        Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
        Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
        Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
    }

    [TestMethod]
    public async Task Call__Anonymous_contract_can_receive_console_messages()
    {
        // Arrange
        var conn = new TestPhormConnection("")
        {
            DefaultSchema = "schema"
        };

        var cmd = new TestDbCommand();
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn)
        {
            ConsoleMessageCaptureProvider = (s, g) => new TestConsoleMessageCapture(s, g)
        };

        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
        phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

        var arg = new
        {
            Arg = 1,
            ConsoleLogs = ContractMember.Console()
        };

        var runner = new PhormContractRunner<IConsoleLogContract>(phorm, null, DbObjectType.Default, arg, null);

        // Act
        var res = await runner.CallAsync(TestContext.CancellationToken);

        // Assert
        Assert.AreEqual(1, res);

        Assert.HasCount(3, arg.ConsoleLogs.Value);
        Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
        Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
        Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
    }

    #endregion Console messages
}