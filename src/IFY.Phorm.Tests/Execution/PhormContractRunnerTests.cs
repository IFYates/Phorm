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
    public class PhormContractRunnerTests
    {
        public interface IContractDTO : IPhormContract
        {
        }
        [PhormContract(Namespace = "schema", Name = "contractName", Target = DbObjectType.Table)]
        public interface IContractWithAttributeDTO : IPhormContract
        {
        }
        [DataContract(Namespace = "schema", Name = "contractName")]
        public class DataContractDTO : IPhormContract
        {
        }

        private static T? getFieldValue<T>(object obj, string fieldName)
        {
            return (T?)obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
        }

        public class TestDto
        {
            public string? Value { get; set; }
        }

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

        #region Constructor

        [TestMethod]
        public void PhormContractRunner__Anonymous_Gets_contract_info()
        {
            // Act
            var runner = new PhormContractRunner<IPhormContract>(null, "objectName", DbObjectType.Table);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("objectName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Anonymous_No_name__Exception()
        {
            // Act
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                _ = new PhormContractRunner<IPhormContract>(null, null, DbObjectType.StoredProcedure);
            });
        }

        [TestMethod]
        public void PhormContractRunner__Anonymous_Default_objectType_is_StoredProcedure()
        {
            // Act
            var runner = new PhormContractRunner<IPhormContract>(null, "objectName", DbObjectType.Default);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("objectName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract__Takes_value()
        {
            // Act
            var runner = new PhormContractRunner<IContractDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract__Ignores_name_override()
        {
            // Act
            var runner = new PhormContractRunner<IContractDTO>(null, "objectName", DbObjectType.Table);

            // Assert
            Assert.IsNull(getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract_with_attribute__Takes_values()
        {
            // Act
            var runner = new PhormContractRunner<IContractWithAttributeDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__Contract_with_attribute__Ignores_overrides()
        {
            // Act
            var runner = new PhormContractRunner<IContractWithAttributeDTO>(null, "objectName", DbObjectType.View);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__DataContract__Takes_values()
        {
            // Act
            var runner = new PhormContractRunner<DataContractDTO>(null, null, DbObjectType.Default);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        [TestMethod]
        public void PhormContractRunner__DataContract__Ignores_name_override()
        {
            // Act
            var runner = new PhormContractRunner<DataContractDTO>(null, "objectName", DbObjectType.View);

            // Assert
            Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
            Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
            Assert.AreEqual(DbObjectType.View, getFieldValue<DbObjectType>(runner, "_objectType"));
        }

        #endregion Constructor

        #region Many

        [TestMethod]
        public void Many__Procedure_by_anon_object()
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.Many<TestDto>(new { Arg = 1 });

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[ContractName]", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        public void ManyAsync__Procedure_by_anon_contract()
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.ManyAsync<TestDto>(new { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[ContractName]", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void Many__NonProcedure_by_anon_object(DbObjectType objType)
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.Many<TestDto>(new { Arg1 = 1, Arg2 = 2 });

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[ContractName] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(3, pars.Length);
            Assert.AreEqual("@Arg1", pars[0].ParameterName);
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[2].Direction);
            Assert.AreEqual(1, pars[2].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void ManyAsync__NonProcedure_by_anon_object(DbObjectType objType)
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.Many<TestDto>(new { Arg1 = 1, Arg2 = 2 });

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[ContractName] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(3, pars.Length);
            Assert.AreEqual("@Arg1", pars[0].ParameterName);
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[2].Direction);
            Assert.AreEqual(1, pars[2].Value);
        }

        [TestMethod]
        public void Many__Procedure_by_contract()
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.Many<TestDto>(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[TestContract]", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        public void ManyAsync__Procedure_by_contract()
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.ManyAsync<TestDto>(new TestContract { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[TestContract]", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void Many__NonProcedure_by_contract(DbObjectType objType)
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.Many<TestDto>(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[TestContract] WHERE [Arg] = @Arg", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void ManyAsync__NonProcedure_by_contract(DbObjectType objType)
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value2"
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value3"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.ManyAsync<TestDto>(new TestContract { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(3, res.Length);
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[TestContract] WHERE [Arg] = @Arg", cmd.CommandText);
            Assert.AreEqual("value1", res[0].Value);
            Assert.AreEqual("value2", res[1].Value);
            Assert.AreEqual("value3", res[2].Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        public class TestSecureDto
        {
            public int Arg { get; set; }
            [SecureValue("class", nameof(Arg))]
            public string Arg3 { get; set; }
        }

        [TestMethod]
        public void Many__SecureValue_sent_encrypted_received_decrypted_by_authenticator()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

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

            var rdr = new TestDbReader();
            rdr.Data.Add(new Dictionary<string, object>()
            {
                ["Arg3"] = encdata
            });
            var cmd = new TestDbCommand
            {
                Reader = rdr
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default);

            var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

            // Act
            var res = runner.Many<TestSecureDto>(dto);

            // Assert
            Assert.AreEqual(1, res.Length);
            Assert.AreEqual("secure_value", res[0].Arg3);
            CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
        }

        #endregion Many

        #region One

        [TestMethod]
        public void Onw__Multiple_records__Exception()
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
                    },
                    new Dictionary<string, object>
                    {
                        ["Value"] = "value1"
                    }
                }
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            Assert.ThrowsException<InvalidOperationException>(() => runner.One<TestDto>(new { Arg = 1 }));
        }

        [TestMethod]
        public void One__Multiple_resultsets__Exception()
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
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["Value"] = "value1"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            Assert.ThrowsException<InvalidOperationException>(() => runner.One<TestDto>(new { Arg = 1 }));
        }

        [TestMethod]
        public void One__Procedure_by_anon_object()
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.One<TestDto>(new { Arg = 1 });

            // Assert
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[ContractName]", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        public void OneAsync__Procedure_by_anon_contract()
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.OneAsync<TestDto>(new { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[ContractName]", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void One__NonProcedure_by_anon_object(DbObjectType objType)
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.One<TestDto>(new { Arg1 = 1, Arg2 = 2 });

            // Assert
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[ContractName] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(3, pars.Length);
            Assert.AreEqual("@Arg1", pars[0].ParameterName);
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[2].Direction);
            Assert.AreEqual(1, pars[2].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void OneAsync__NonProcedure_by_anon_object(DbObjectType objType)
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.One<TestDto>(new { Arg1 = 1, Arg2 = 2 });

            // Assert
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[ContractName] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(3, pars.Length);
            Assert.AreEqual("@Arg1", pars[0].ParameterName);
            Assert.AreEqual("@Arg2", pars[1].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[2].Direction);
            Assert.AreEqual(1, pars[2].Value);
        }

        [TestMethod]
        public void One__Procedure_by_contract()
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.One<TestDto>(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[TestContract]", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        public void OneAsync__Procedure_by_contract()
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.OneAsync<TestDto>(new TestContract { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual("[schema].[TestContract]", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void One__NonProcedure_by_contract(DbObjectType objType)
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.One<TestDto>(new TestContract { Arg = 1 });

            // Assert
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[TestContract] WHERE [Arg] = @Arg", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        [DataRow(DbObjectType.Table)]
        [DataRow(DbObjectType.View)]
        public void OneAsync__NonProcedure_by_contract(DbObjectType objType)
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType);

            // Act
            var res = runner.OneAsync<TestDto>(new TestContract { Arg = 1 }).Result;

            // Assert
            Assert.AreEqual(CommandType.Text, cmd.CommandType);
            Assert.AreEqual("SELECT * FROM [schema].[TestContract] WHERE [Arg] = @Arg", cmd.CommandText);
            Assert.AreEqual("value1", res?.Value);

            var pars = cmd.Parameters.AsParameters();
            Assert.AreEqual(2, pars.Length);
            Assert.AreEqual("@Arg", pars[0].ParameterName);
            Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
            Assert.AreEqual(1, pars[1].Value);
        }

        [TestMethod]
        public void One__SecureValue_sent_encrypted_received_decrypted_by_authenticator()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

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

            var rdr = new TestDbReader();
            rdr.Data.Add(new Dictionary<string, object>()
            {
                ["Arg3"] = encdata
            });
            var cmd = new TestDbCommand
            {
                Reader = rdr
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default);

            var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

            // Act
            var res = runner.One<TestSecureDto>(dto);

            // Assert
            Assert.AreEqual("secure_value", res.Arg3);
            CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
        }

        #endregion One
    }
}