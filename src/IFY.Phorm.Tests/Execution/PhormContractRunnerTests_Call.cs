using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Call
    {
        public class TestContract : IPhormContract, IMemberTestContract
        {
            public int Arg { get; set; }
            [IgnoreDataMember]
            public int Arg2 { get; set; }
            [IgnoreDataMember]
            public string? Arg3 { get; set; } = string.Empty;
            [IgnoreDataMember]
            public ContractMember Arg4 { get; set; } = ContractMember.Out<string>("InvalidRename");
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

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call_by_anon_object(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure);

            // Act
            var res = byAsync
                ? runner.CallAsync(new { Arg = 1 }).Result
                : runner.Call(new { Arg = 1 });

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[CallTest]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call_by_contract(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, null, DbObjectType.StoredProcedure);

            // Act
            var res = byAsync
                ? runner.CallAsync(new TestContract { Arg = 1 }).Result
                : runner.Call(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[TestContract]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.InputOutput, pars[0].Direction);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__Contract_and_arg_rename(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default);

            // Act
            var res = byAsync
                ? runner.CallAsync(new TestContract { Arg = 1 }).Result
                : runner.Call(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[otherSchema].[IAmRenamedContract]", cmd.CommandText);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual("@RenamedArg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.Input, pars[0].Direction);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__Out_arg(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default);

            // Act
            var res = byAsync
                ? runner.CallAsync(new TestContract { Arg = 1 }).Result
                : runner.Call(new TestContract { Arg = 1 });

            // Assert
            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.Output, pars[1].Direction);
            Assert.AreEqual("@Arg4", pars[3].ParameterName);
            Assert.AreEqual(ParameterDirection.Output, pars[3].Direction);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__Required_arg_null__Exception(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IMemberTestContract>(phorm, null, DbObjectType.Default);

            // Act
            if (byAsync)
            {
                Assert.ThrowsException<AggregateException>(() => runner.CallAsync(new TestContract { Arg3 = null }).Result);
            }
            else
            {
                Assert.ThrowsException<ArgumentNullException>(() => runner.Call(new TestContract { Arg3 = null }));
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__SecureValue_sent_encrypted_received_decrypted_by_authenticator(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand();
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

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

            var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default);

            var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

            // Act
            var res = byAsync
                ? runner.CallAsync(dto).Result
                : runner.Call(dto);

            // Assert
            Assert.AreEqual(1, res);
            Assert.AreEqual("secure_value", dto.Arg3);
            CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Call__Returns_result__Exception(bool byAsync)
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand(new TestDbReader
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

            var phorm = new TestPhormRunner(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure);

            // Act
            if (byAsync)
            {
                Assert.ThrowsException<AggregateException>(() => runner.CallAsync(new { Arg = 1 }).Result);
            }
            else
            {
                Assert.ThrowsException<InvalidOperationException>(() => runner.Call(new { Arg = 1 }));
            }
        }
    }
}