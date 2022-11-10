using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;

namespace IFY.Phorm.Execution.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Call
    {
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

        [TestInitialize]
        public void Init()
        {
            AbstractPhormSession.ResetConnectionPool();
        }

        [TestMethod]
        public void Call_by_anon_object()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure, new { Arg = 1 });

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[usp_CallTest]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        }

        [TestMethod]
        public void Call_by_contract()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<TestContract>(phorm, null, DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.InputOutput, pars[0].Direction);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        }

        [TestMethod]
        public void Call__Contract_and_arg_rename()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, new TestContract { Arg = 1 });

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[otherSchema].[usp_IAmRenamedContract]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual("@RenamedArg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
        }

        [TestMethod]
        public void Call__Concrete_contract_with_input_and_output()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<TestContract2>(phorm, null, DbObjectType.Default, new TestContract2());

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(4, pars.Length); // + return
            Assert.AreEqual("@Input", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
            Assert.AreEqual("@Output", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.Output, pars[1].Direction);
            Assert.AreEqual("@InputOutput", pars[2].ParameterName);
            Assert.AreEqual(ParameterDirection.InputOutput, pars[2].Direction);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[3].Direction);
        }

        [TestMethod]
        public void Call__Out_arg()
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

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, args);

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.Output, pars[1].Direction);
            Assert.AreEqual("@Arg4", pars[3].ParameterName);
            Assert.AreEqual(ParameterDirection.Output, pars[3].Direction);
            Assert.AreSame(cm, args.Arg4);
        }

        [TestMethod]
        public void Call__Required_arg_null__Exception()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default, new TestContract { Arg3 = null! });

            // Act
            Assert.ThrowsException<AggregateException>(() => runner.CallAsync().Result);
        }

        [TestMethod]
        public void Call__SecureValue_sent_encrypted_received_decrypted_by_authenticator()
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
            var encrMock = new Mock<IEncryptor>(MockBehavior.Strict);
            encrMock.SetupProperty(m => m.Authenticator);
            encrMock.Setup(m => m.Encrypt(data))
                .Returns(encdata);
            encrMock.Setup(m => m.Decrypt(encdata))
                .Returns(data);

            var provMock = new Mock<IEncryptionProvider>(MockBehavior.Strict);
            provMock.Setup(m => m.GetInstance("class"))
                .Returns(() => encrMock.Object);
            GlobalSettings.EncryptionProvider = provMock.Object;

            var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

            var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default, dto);

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual("secure_value", dto.Arg3);
            CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
        }

        [TestMethod]
        public void Call__Returns_result__Exception()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand(new TestDbDataReader
            {
                Data = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value1"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure, new { Arg = 1 });

            // Act
            var ex = (InvalidOperationException)Assert.ThrowsException<AggregateException>(() =>
            {
                _ = runner.CallAsync().Result;
            }).InnerException!;

            // Assert
            Assert.AreEqual("Non-result request returned a result.", ex.Message);
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
        public void Call__Contract_can_receive_console_messages()
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

            var runner = new PhormContractRunner<IConsoleLogContract>(phorm, null, DbObjectType.Default, arg);

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);

            Assert.AreEqual(3, arg.ConsoleLogs.Value.Length);
            Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
            Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
            Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
        }

        [TestMethod]
        public void Call__Anonymous_contract_can_receive_console_messages()
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

            var runner = new PhormContractRunner<IConsoleLogContract>(phorm, null, DbObjectType.Default, arg);

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);

            Assert.AreEqual(3, arg.ConsoleLogs.Value.Length);
            Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
            Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
            Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
        }

        #endregion Console messages
    }
}