using IFY.Phorm.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Resultsets
    {
        class TestParent
        {
            public long Id { get; set; }
            public string? Key { get; set; }

            [Resultset(0, nameof(ChildrenSelector))]
            public TestChild[] Children { get; set; } = Array.Empty<TestChild>();
            public static RecordMatcher<TestParent, TestChild> ChildrenSelector { get; } = new((p, c) => c.ParentId == p.Id);

            [Resultset(1, nameof(ChildrenSelector))]
            public TestChild? Child { get; set; }
        }

        class TestChild
        {
            public long ParentId { get; set; }
            public string? Value { get; set; }
        }

        class TestParentBadProperty
        {
            [Resultset(0, nameof(TrueSelector))]
            public TestChild[] Children { get; } = Array.Empty<TestChild>();
            public static RecordMatcher<TestParentBadProperty, TestChild> TrueSelector { get; } = new((p, c) => true);
        }

        public interface ITestContract : IPhormContract
        {
        }

        [TestMethod]
        public void ManyAsync__Resolves_secondary_resultset()
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
                        ["Id"] = 1,
                        ["Key"] = "key1"
                    }
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value1"
                        },
                        new Dictionary<string, object>
                        {
                            ["Value"] = "value2"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.ManyAsync<TestParent>().Result;

            // Assert
            Assert.AreEqual(1, res[0].Children.Length);
            Assert.AreEqual("value1", res[0].Children[0].Value);
        }

        [TestMethod]
        public void ManyAsync__Resultset_property_not_writable__Fail()
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
                        ["Id"] = 1,
                        ["Key"] = "key1"
                    }
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value1"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var ex = Assert.ThrowsException<AggregateException>(() =>
                runner.ManyAsync<TestParentBadProperty>().Result);
            Assert.AreEqual("Resultset property 'Children' is not writable.", ((InvalidDataContractException?)ex.InnerException)?.Message);
        }

        [TestMethod]
        public void One__Resolves_secondary_resultset()
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
                        ["Id"] = 1,
                        ["Key"] = "key1"
                    }
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value1"
                        },
                        new Dictionary<string, object>
                        {
                            ["Value"] = "value2"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.OneAsync<TestParent>().Result;

            // Assert
            Assert.AreEqual(1, res?.Children.Length);
            Assert.AreEqual("value1", res?.Children[0].Value);
        }

        [TestMethod]
        public void One__Multiple_results_for_single_child__Fail()
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
                        ["Id"] = 1,
                        ["Key"] = "key1"
                    }
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    Array.Empty<Dictionary<string, object>>(),
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value1"
                        },
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value2"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var ex = Assert.ThrowsException<AggregateException>(() =>
               runner.OneAsync<TestParent>().Result);
            Assert.IsTrue(((InvalidCastException?)ex.InnerException)?.Message.Contains("not an array") == true);
        }

        [TestMethod]
        public void One__Resolves_single_child_entity()
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
                        ["Id"] = 1,
                        ["Key"] = "key1"
                    }
                },
                Results = new List<Dictionary<string, object>[]>(new[]
                {
                    Array.Empty<Dictionary<string, object>>(),
                    new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["ParentId"] = 1,
                            ["Value"] = "value1"
                        }
                    }
                })
            });
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

            var runner = new PhormContractRunner<ITestContract>(phorm, "ContractName", DbObjectType.StoredProcedure);

            // Act
            var res = runner.OneAsync<TestParent>().Result;

            // Assert
            Assert.AreEqual("value1", res?.Child?.Value);
        }
    }
}