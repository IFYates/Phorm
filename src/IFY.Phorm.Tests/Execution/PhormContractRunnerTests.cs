using IFY.Phorm.Data;
using IFY.Phorm.Encryption;
using IFY.Phorm.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Runtime.Serialization;

namespace IFY.Phorm.Execution.Tests;

[TestClass]
public class PhormContractRunnerTests
{
    interface IContractDTO : IPhormContract
    {
    }
    [PhormContract(Namespace = "schema", Name = "contractName", Target = DbObjectType.Table)]
    interface IContractWithAttributeDTO : IPhormContract
    {
    }
    [DataContract(Namespace = "schema", Name = "contractName")]
    class DataContractDTO : IPhormContract
    {
    }

    private static T? getFieldValue<T>(object obj, string fieldName)
    {
        return (T?)obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(obj);
    }

    class TestDto
    {
        public string? Value { get; set; }
    }

    class TestContract : IPhormContract, IMemberTestContract
    {
        public int Arg { get; set; } = 0;
        [IgnoreDataMember]
        public int Arg2 { get; set; } = 0;
        [IgnoreDataMember]
        public string Arg3 { get; set; } = string.Empty;
        [IgnoreDataMember]
        public ContractMember Arg4 { get; set; } = new ContractMember("InvalidRename", null, ParameterType.Output, typeof(string));
    }

    [PhormContract(Name = "IAmRenamedContract", Namespace = "otherSchema")]
    interface IMemberTestContract : IPhormContract
    {
        [DataMember(Name = "RenamedArg")]
        int Arg { get; }
        int Arg2 { set; } // Out
        [DataMember(IsRequired = true)]
        string Arg3 { get; }
        ContractMember Arg4 { get; }
    }

    interface ISecureTestContract : IPhormContract
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

    #region Constructor

    [TestMethod]
    public void PhormContractRunner__ContractType()
    {
        // Act
        var runner = new PhormContractRunner<IPhormContract>(null!, "objectName", DbObjectType.Table, null);

        // Assert
        Assert.AreEqual(typeof(IPhormContract), ((IPhormContractRunner<IPhormContract>)runner).ContractType);
    }

    [TestMethod]
    public void PhormContractRunner__Anonymous_Gets_contract_info()
    {
        // Act
        var runner = new PhormContractRunner<IPhormContract>(null!, "objectName", DbObjectType.Table, null);

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
            _ = new PhormContractRunner<IPhormContract>(null!, null, DbObjectType.StoredProcedure, null);
        });
    }

    [TestMethod]
    public void PhormContractRunner__Anonymous_Default_objectType_is_StoredProcedure()
    {
        // Act
        var runner = new PhormContractRunner<IPhormContract>(null!, "objectName", DbObjectType.Default, null);

        // Assert
        Assert.IsNull(getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("objectName", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__Contract__Takes_value()
    {
        // Act
        var runner = new PhormContractRunner<IContractDTO>(null!, null, DbObjectType.Default, null);

        // Assert
        Assert.IsNull(getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__Contract__Ignores_name_override()
    {
        // Act
        var runner = new PhormContractRunner<IContractDTO>(null!, "objectName", DbObjectType.Table, null);

        // Assert
        Assert.IsNull(getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("ContractDTO", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__Contract_with_attribute__Takes_values()
    {
        // Act
        var runner = new PhormContractRunner<IContractWithAttributeDTO>(null!, null, DbObjectType.Default, null);

        // Assert
        Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__Contract_with_attribute__Ignores_overrides()
    {
        // Act
        var runner = new PhormContractRunner<IContractWithAttributeDTO>(null!, "objectName", DbObjectType.View, null);

        // Assert
        Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.Table, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__DataContract__Takes_values()
    {
        // Act
        var runner = new PhormContractRunner<DataContractDTO>(null!, null, DbObjectType.Default, null);

        // Assert
        Assert.AreEqual("schema", getFieldValue<string>(runner, "_schema"));
        Assert.AreEqual("contractName", getFieldValue<string>(runner, "_objectName"));
        Assert.AreEqual(DbObjectType.StoredProcedure, getFieldValue<DbObjectType>(runner, "_objectType"));
    }

    [TestMethod]
    public void PhormContractRunner__DataContract__Ignores_name_override()
    {
        // Act
        var runner = new PhormContractRunner<DataContractDTO>(null!, "objectName", DbObjectType.View, null);

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

        var cmd = new TestDbCommand(new TestDbDataReader
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        var res = runner.Get<TestDto[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);
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

        var cmd = new TestDbCommand(new TestDbDataReader
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto[]>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);
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
    [DataRow(DbObjectType.Table, "ContractName")]
    [DataRow(DbObjectType.View, "vw_ContractName")]
    public void Many__NonProcedure_by_anon_object(DbObjectType objType, string actName)
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType, new { Arg1 = 1, Arg2 = 2 });

        // Act
        var res = runner.Get<TestDto[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
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
    [DataRow(DbObjectType.Table, "ContractName")]
    [DataRow(DbObjectType.View, "vw_ContractName")]
    public void ManyAsync__NonProcedure_by_anon_object(DbObjectType objType, string actName)
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType, new { Arg1 = 1, Arg2 = 2 });

        // Act
        var res = runner.Get<TestDto[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
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

        var cmd = new TestDbCommand(new TestDbDataReader
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.Get<TestDto[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);
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

        var cmd = new TestDbCommand(new TestDbDataReader
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto[]>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);
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
    [DataRow(DbObjectType.Table, "TestContract")]
    [DataRow(DbObjectType.View, "vw_TestContract")]
    public void Many__NonProcedure_by_contract(DbObjectType objType, string actName)
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType, new TestContract { Arg = 1 });

        // Act
        var res = runner.Get<TestDto[]>()!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg] = @Arg", cmd.CommandText);
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
    [DataRow(DbObjectType.Table, "TestContract")]
    [DataRow(DbObjectType.View, "vw_TestContract")]
    public void ManyAsync__NonProcedure_by_contract(DbObjectType objType, string actName)
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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto[]>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(3, res.Length);
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg] = @Arg", cmd.CommandText);
        Assert.AreEqual("value1", res[0].Value);
        Assert.AreEqual("value2", res[1].Value);
        Assert.AreEqual("value3", res[2].Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }

    class TestSecureDto
    {
        public int Arg { get; set; }
        [SecureValue("class", nameof(Arg))]
        public string Arg3 { get; set; } = string.Empty;
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

        var rdr = new TestDbDataReader();
        rdr.Data.Add(new Dictionary<string, object>()
        {
            ["Arg"] = 100,
            ["Arg3"] = encdata
        });
        var cmd = new TestDbCommand
        {
            Reader = rdr
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

        var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default, dto);

        // Act
        var res = runner.Get<TestSecureDto[]>()!;

        // Assert
        Assert.AreEqual(1, res.Length);
        Assert.AreEqual("secure_value", res[0].Arg3);
        CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
    }

    [TestMethod]
    public void ManyAsync_IEnumerable__Result_not_resolved()
    {
        static int getUnresolvedEntityCount<T>(IEnumerable<T> l)
        {
            var resolversField = typeof(EntityList<T>)
                .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)resolversField.GetValue(l)!).Count;
        }

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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<IEnumerable<TestDto>>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(3, res.Count());
        Assert.AreEqual(3, getUnresolvedEntityCount(res));
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);

        var arr = res.ToArray();
        Assert.AreEqual("value1", arr[0].Value);
        Assert.AreEqual("value2", arr[1].Value);
        Assert.AreEqual("value3", arr[2].Value);
        Assert.AreEqual(0, getUnresolvedEntityCount(res));
    }

    [TestMethod]
    public void ManyAsync_ICollection__Result_not_resolved()
    {
        static int getUnresolvedEntityCount<T>(IEnumerable<T> l)
        {
            var resolversField = typeof(EntityList<T>)
                .GetField("_resolvers", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return ((ICollection)resolversField.GetValue(l)!).Count;
        }

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

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<ICollection<TestDto>>(CancellationToken.None).Result!;

        // Assert
        Assert.AreEqual(3, res.Count());
        Assert.AreEqual(3, getUnresolvedEntityCount(res));
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);

        var arr = res.ToArray();
        Assert.AreEqual("value1", arr[0].Value);
        Assert.AreEqual("value2", arr[1].Value);
        Assert.AreEqual("value3", arr[2].Value);
        Assert.AreEqual(0, getUnresolvedEntityCount(res));
    }

    #endregion Many

    #region One

    [TestMethod]
    public void One__Supports_DBNull()
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
                    ["Value"] = DBNull.Value
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        var obj = runner.Get<TestDto>()!;

        // Assert
        Assert.IsNull(obj.Value);
    }

    [TestMethod]
    public void One__Multiple_records__Exception()
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
                },
                new Dictionary<string, object>
                {
                    ["Value"] = "value1"
                }
            }
        });
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        Assert.ThrowsException<InvalidOperationException>(() => runner.Get<TestDto>());
    }

    [TestMethod]
    public void One__Procedure_by_anon_object()
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

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        var res = runner.Get<TestDto>();

        // Assert
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);
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

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_ContractName]", cmd.CommandText);
        Assert.AreEqual("value1", res?.Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }

    [TestMethod]
    [DataRow(DbObjectType.Table, "ContractName")]
    [DataRow(DbObjectType.View, "vw_ContractName")]
    public void One__NonProcedure_by_anon_object(DbObjectType objType, string actName)
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

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType, new { Arg1 = 1, Arg2 = 2 });

        // Act
        var res = runner.Get<TestDto>();

        // Assert
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
        Assert.AreEqual("value1", res?.Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(3, pars.Length);
        Assert.AreEqual("@Arg1", pars[0].ParameterName);
        Assert.AreEqual("@Arg2", pars[1].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[2].Direction);
        Assert.AreEqual(1, pars[2].Value);
    }

    [TestMethod]
    [DataRow(DbObjectType.Table, "ContractName")]
    [DataRow(DbObjectType.View, "vw_ContractName")]
    public void OneAsync__NonProcedure_by_anon_object(DbObjectType objType, string actName)
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

        var runner = new PhormContractRunner<IPhormContract>(phorm, "ContractName", objType, new { Arg1 = 1, Arg2 = 2 });

        // Act
        var res = runner.Get<TestDto>();

        // Assert
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg1] = @Arg1 AND [Arg2] = @Arg2", cmd.CommandText);
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

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.Get<TestDto>();

        // Assert
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);
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

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", DbObjectType.StoredProcedure, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
        Assert.AreEqual("[schema].[usp_TestContract]", cmd.CommandText);
        Assert.AreEqual("value1", res?.Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }

    [TestMethod]
    [DataRow(DbObjectType.Table, "TestContract")]
    [DataRow(DbObjectType.View, "vw_TestContract")]
    public void One__NonProcedure_by_contract(DbObjectType objType, string actName)
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

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType, new TestContract { Arg = 1 });

        // Act
        var res = runner.Get<TestDto>();

        // Assert
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg] = @Arg", cmd.CommandText);
        Assert.AreEqual("value1", res?.Value);

        var pars = cmd.Parameters.AsParameters();
        Assert.AreEqual(2, pars.Length);
        Assert.AreEqual("@Arg", pars[0].ParameterName);
        Assert.AreEqual(ParameterDirection.ReturnValue, pars[1].Direction);
        Assert.AreEqual(1, pars[1].Value);
    }

    [TestMethod]
    [DataRow(DbObjectType.Table, "TestContract")]
    [DataRow(DbObjectType.View, "vw_TestContract")]
    public void OneAsync__NonProcedure_by_contract(DbObjectType objType, string actName)
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

        var runner = new PhormContractRunner<TestContract>(phorm, "ContractName", objType, new TestContract { Arg = 1 });

        // Act
        var res = runner.GetAsync<TestDto>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(CommandType.Text, cmd.CommandType);
        Assert.AreEqual("SELECT * FROM [schema].[" + actName + "] WHERE [Arg] = @Arg", cmd.CommandText);
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

        var mocks = new MockRepository(MockBehavior.Strict);

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

        var rdr = new TestDbDataReader();
        rdr.Data.Add(new Dictionary<string, object>()
        {
            ["Arg"] = 100,
            ["Arg3"] = encdata
        });
        var cmd = new TestDbCommand
        {
            Reader = rdr
        };
        conn.CommandQueue.Enqueue(cmd);

        var phorm = new TestPhormSession(conn);

        var dto = new TestContract { Arg = 100, Arg3 = "secure_value" };

        var runner = new PhormContractRunner<ISecureTestContract>(phorm, null, DbObjectType.Default, dto);

        // Act
        var res = runner.Get<TestSecureDto>()!;

        // Assert
        mocks.Verify();
        Assert.AreEqual("secure_value", res.Arg3);
        CollectionAssert.AreEqual(100.GetBytes(), encrMock.Object.Authenticator);
    }

    #endregion One

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
    public void Get__Contract_can_receive_console_messages()
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
        _ = runner.GetAsync<object>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(3, arg.ConsoleLogs.Value.Length);
        Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
        Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
        Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
    }

    [TestMethod]
    public void Get__Anonymous_contract_can_receive_console_messages()
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
        _ = runner.GetAsync<object>(CancellationToken.None).Result;

        // Assert
        Assert.AreEqual(3, arg.ConsoleLogs.Value.Length);
        Assert.AreEqual("Message1", arg.ConsoleLogs.Value[0].Message);
        Assert.AreEqual("Message2", arg.ConsoleLogs.Value[1].Message);
        Assert.AreEqual("Message3", arg.ConsoleLogs.Value[2].Message);
    }

    #endregion Console messages
}