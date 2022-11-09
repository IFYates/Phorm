using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using IFY.Phorm.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Events
    {
        private int _unwantedInvocations = 0;
        private Action<object?, ConnectedEventArgs> _globalConnected = null!;
        private void invokeHandler(object? sender, ConnectedEventArgs args) => _globalConnected?.Invoke(sender, args);
        private Action<object?, CommandExecutingEventArgs> _globalCommandExecuting = null!;
        private void invokeHandler(object? sender, CommandExecutingEventArgs args) => _globalCommandExecuting?.Invoke(sender, args);
        private Action<object?, CommandExecutedEventArgs> _globalCommandExecuted = null!;
        private void invokeHandler(object? sender, CommandExecutedEventArgs args) => _globalCommandExecuted?.Invoke(sender, args);
        private Action<object?, UnexpectedRecordColumnEventArgs> _globalUnexpectedRecordColumn = null!;
        private void invokeHandler(object? sender, UnexpectedRecordColumnEventArgs args) => _globalUnexpectedRecordColumn?.Invoke(sender, args);
        private Action<object?, UnresolvedContractMemberEventArgs> _globalUnresolvedContractMember = null!;
        private void invokeHandler(object? sender, UnresolvedContractMemberEventArgs args) => _globalUnresolvedContractMember?.Invoke(sender, args);
        private Action<object?, ConsoleMessageEventArgs> _globalConsoleMessage = null!;
        private void invokeHandler(object? sender, ConsoleMessageEventArgs args) => _globalConsoleMessage?.Invoke(sender, args);

        public class TestEntity
        {
            public string Getter { get; } = string.Empty;
            public string GetterSetter { get; set; } = string.Empty;
            public string Setter { private get; set; } = string.Empty;
        }

        [TestInitialize]
        public void Init()
        {
            AbstractPhormSession.ResetConnectionPool();

            Events.Connected += invokeHandler;
            Events.CommandExecuting += invokeHandler;
            Events.CommandExecuted += invokeHandler;
            Events.UnexpectedRecordColumn += invokeHandler;
            Events.UnresolvedContractMember += invokeHandler;
            Events.ConsoleMessage += invokeHandler;
        }
        [TestCleanup]
        public void Clean()
        {
            Events.Connected -= invokeHandler;
            Events.CommandExecuting -= invokeHandler;
            Events.CommandExecuted -= invokeHandler;
            Events.UnexpectedRecordColumn -= invokeHandler;
            Events.UnresolvedContractMember -= invokeHandler;
            Events.ConsoleMessage -= invokeHandler;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private void eventInvokeFail(object? sender, System.EventArgs args)
        {
            ++_unwantedInvocations;
            Assert.Fail();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void OnConnected__Ignores_exceptions(bool isGlobal)
        {
            // Arrange
            var phorm = new TestPhormSession();

            var wasCalled = false;
            if (isGlobal)
            {
                _globalConnected = (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }
            else
            {
                phorm.Connected += (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }

            // Act
            phorm.OnConnected(new ConnectedEventArgs());

            // Assert
            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void OnCommandExecuting__Ignores_exceptions(bool isGlobal)
        {
            // Arrange
            var phorm = new TestPhormSession();

            var wasCalled = false;
            if (isGlobal)
            {
                _globalCommandExecuting = (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }
            else
            {
                phorm.CommandExecuting += (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }

            // Act
            phorm.OnCommandExecuting(new CommandExecutingEventArgs());

            // Assert
            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void OnCommandExecuted__Ignores_exceptions(bool isGlobal)
        {
            // Arrange
            var phorm = new TestPhormSession();

            var wasCalled = false;
            if (isGlobal)
            {
                _globalCommandExecuted = (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }
            else
            {
                phorm.CommandExecuted += (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }

            // Act
            phorm.OnCommandExecuted(new CommandExecutedEventArgs());

            // Assert
            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void OnUnexpectedRecordColumn__Ignores_exceptions(bool isGlobal)
        {
            // Arrange
            var phorm = new TestPhormSession();

            var wasCalled = false;
            if (isGlobal)
            {
                _globalUnexpectedRecordColumn = (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }
            else
            {
                phorm.UnexpectedRecordColumn += (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }

            // Act
            phorm.OnUnexpectedRecordColumn(new UnexpectedRecordColumnEventArgs());

            // Assert
            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void OnUnresolvedContractMember__Ignores_exceptions(bool isGlobal)
        {
            // Arrange
            var phorm = new TestPhormSession();

            var wasCalled = false;
            if (isGlobal)
            {
                _globalUnresolvedContractMember = (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }
            else
            {
                phorm.UnresolvedContractMember += (_, __) =>
                {
                    wasCalled = true;
                    throw new Exception();
                };
            }

            // Act
            phorm.OnUnresolvedContractMember(new UnresolvedContractMemberEventArgs());

            // Assert
            Assert.IsTrue(wasCalled);
        }

        [TestMethod]
        public void Call__Invokes_both_CommandExecuting_events_before_executing()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand
            {
                OnExecuteReaderAsync = () => throw new NotImplementedException()
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, CommandExecutingEventArgs args)? instanceEvent = null;
            phorm.CommandExecuting += (object? sender, CommandExecutingEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, CommandExecutingEventArgs args)? globalEvent = null;
            _globalCommandExecuting = (object? sender, CommandExecutingEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            phorm.CommandExecuted += eventInvokeFail;
            _globalCommandExecuted = eventInvokeFail;

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            _ = (NotImplementedException)Assert.ThrowsException<AggregateException>(() =>
            {
                _ = runner.CallAsync().Result;
            }).InnerException!;

            // Assert
            Assert.AreEqual(0, _unwantedInvocations);
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
            Assert.AreEqual(3, instanceEvent.Value.args.CommandParameters.Count); // + return
            Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
            Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
        }

        [TestMethod]
        public void Call__Invokes_both_CommandExecuted_events_after_execution()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand
            {
                ReturnValue = DateTime.UtcNow.Millisecond
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, CommandExecutedEventArgs args)? instanceEvent = null;
            phorm.CommandExecuted += (object? sender, CommandExecutedEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, CommandExecutedEventArgs args)? globalEvent = null;
            _globalCommandExecuted = (object? sender, CommandExecutedEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            Guid? commandGuid = null;
            phorm.CommandExecuting += (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };
            _globalCommandExecuting = (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
            Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
            Assert.AreEqual(3, instanceEvent.Value.args.CommandParameters.Count); // + return
            Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
            Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
            Assert.IsFalse(instanceEvent.Value.args.ResultCount.HasValue);
            Assert.AreEqual(res, instanceEvent.Value.args.ReturnValue);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
        }

        [TestMethod]
        public void Get__Invokes_both_CommandExecuting_events_before_executing()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema"
            };

            var cmd = new TestDbCommand
            {
                OnExecuteReaderAsync = () => throw new NotImplementedException()
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, CommandExecutingEventArgs args)? instanceEvent = null;
            phorm.CommandExecuting += (object? sender, CommandExecutingEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, CommandExecutingEventArgs args)? globalEvent = null;
            _globalCommandExecuting = (object? sender, CommandExecutingEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            phorm.CommandExecuted += eventInvokeFail;
            _globalCommandExecuted = eventInvokeFail;

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            Assert.ThrowsException<NotImplementedException>(() =>
            {
                _ = runner.Get<object>();
            });

            // Assert
            Assert.AreEqual(0, _unwantedInvocations);
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
            Assert.AreEqual(3, instanceEvent.Value.args.CommandParameters.Count); // + return
            Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
            Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
        }

        [TestMethod]
        public void Get__Invokes_both_CommandExecuted_events_after_execution()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema",
            };

            var cmd = new TestDbCommand(new TestDbDataReader
            {
                Data = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                }
            })
            {
                ReturnValue = DateTime.UtcNow.Millisecond
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, CommandExecutedEventArgs args)? instanceEvent = null;
            phorm.CommandExecuted += (object? sender, CommandExecutedEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, CommandExecutedEventArgs args)? globalEvent = null;
            _globalCommandExecuted = (object? sender, CommandExecutedEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            Guid? commandGuid = null;
            phorm.CommandExecuting += (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };
            _globalCommandExecuting = (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            var res = runner.Get<object[]>()!;

            // Assert
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
            Assert.AreEqual("[schema].[usp_CallTest]", instanceEvent.Value.args.CommandText);
            Assert.AreEqual(3, instanceEvent.Value.args.CommandParameters.Count); // + return
            Assert.AreEqual(1, (int)instanceEvent.Value.args.CommandParameters["@Arg1"]!);
            Assert.AreEqual("2", (string)instanceEvent.Value.args.CommandParameters["@Arg2"]!);
            Assert.AreEqual(res.Length, instanceEvent.Value.args.ResultCount);
            Assert.AreEqual(cmd.ReturnValue, instanceEvent.Value.args.ReturnValue);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent.Value.args);
        }

        [TestMethod]
        public void Get__Unmapped_record_column__Invokes_both_UnexpectedRecordColumn_events()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema",
            };

            var cmd = new TestDbCommand(new TestDbDataReader
            {
                Data = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["Column"] = "data"
                    }
                }
            })
            {
                ReturnValue = DateTime.UtcNow.Millisecond
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, UnexpectedRecordColumnEventArgs args)? instanceEvent = null;
            phorm.UnexpectedRecordColumn += (object? sender, UnexpectedRecordColumnEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, UnexpectedRecordColumnEventArgs args)? globalEvent = null;
            _globalUnexpectedRecordColumn = (object? sender, UnexpectedRecordColumnEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            Guid? commandGuid = null;
            phorm.CommandExecuting += (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };
            _globalCommandExecuting = (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            var res = runner.Get<TestEntity[]>()!;

            // Assert
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
            Assert.AreEqual(typeof(TestEntity), instanceEvent.Value.args.EntityType);
            Assert.AreEqual("Column", instanceEvent.Value.args.ColumnName);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent!.Value.args);
        }

        [TestMethod]
        public void Get__Unused_entity_member__Invokes_both_UnresolvedContractMember_events()
        {
            // Arrange
            var conn = new TestPhormConnection("")
            {
                DefaultSchema = "schema",
            };

            var cmd = new TestDbCommand(new TestDbDataReader
            {
                Data = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>()
                }
            })
            {
                ReturnValue = DateTime.UtcNow.Millisecond
            };
            conn.CommandQueue.Enqueue(cmd);

            var phorm = new TestPhormSession(conn);

            (object? sender, UnresolvedContractMemberEventArgs args)? instanceEvent = null;
            phorm.UnresolvedContractMember += (object? sender, UnresolvedContractMemberEventArgs args) =>
            {
                instanceEvent = (sender, args);
            };

            (object? sender, UnresolvedContractMemberEventArgs args)? globalEvent = null;
            _globalUnresolvedContractMember = (object? sender, UnresolvedContractMemberEventArgs args) =>
            {
                globalEvent = (sender, args);
            };

            Guid? commandGuid = null;
            phorm.CommandExecuting += (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };
            _globalCommandExecuting = (_, a) =>
            {
                Assert.AreEqual(commandGuid ??= a.CommandGuid, a.CommandGuid);
                Assert.IsNull(instanceEvent);
                Assert.IsNull(globalEvent);
            };

            var runner = new PhormContractRunner<IPhormContract>(phorm, "CallTest", DbObjectType.StoredProcedure,
                new { Arg1 = 1, Arg2 = "2" });

            // Act
            var res = runner.Get<TestEntity>()!;

            // Assert
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
            Assert.AreEqual(typeof(TestEntity), instanceEvent.Value.args.EntityType);
            Assert.AreEqual(2, instanceEvent.Value.args.MemberNames.Length);
            Assert.AreEqual("GetterSetter", instanceEvent.Value.args.MemberNames[0]);
            Assert.AreEqual("Setter", instanceEvent.Value.args.MemberNames[1]);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent!.Value.args);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void Call__Events_can_receive_console_messages(bool asGlobal)
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

            var consoleMessages = new List<ConsoleMessage>();
            if (asGlobal)
            {
                _globalConsoleMessage = (_, a) => consoleMessages.Add(a.ConsoleMessage);
            }
            else
            {
                phorm.ConsoleMessage += (_, a) => consoleMessages.Add(a.ConsoleMessage);
            }

            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

            var runner = new PhormContractRunner<IPhormContract>(phorm, "Test", DbObjectType.Default, null);

            // Act
            var res = runner.CallAsync().Result;

            // Assert
            Assert.AreEqual(1, res);

            Assert.AreEqual(3, consoleMessages.Count);
            Assert.AreEqual("Message1", consoleMessages[0].Message);
            Assert.AreEqual("Message2", consoleMessages[1].Message);
            Assert.AreEqual("Message3", consoleMessages[2].Message);
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true, DisplayName = "Global")]
        public void Get__Events_can_receive_console_messages(bool asGlobal)
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

            var consoleMessages = new List<ConsoleMessage>();
            if (asGlobal)
            {
                _globalConsoleMessage = (_, a) => consoleMessages.Add(a.ConsoleMessage);
            }
            else
            {
                phorm.ConsoleMessage += (_, a) => consoleMessages.Add(a.ConsoleMessage);
            }

            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message1" });
            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message2" });
            phorm.ConsoleMessages.Add(new ConsoleMessage { Message = "Message3" });

            var runner = new PhormContractRunner<IPhormContract>(phorm, "Test", DbObjectType.Default, null);

            // Act
            _ = runner.GetAsync<object>().Result;

            // Assert
            Assert.AreEqual(3, consoleMessages.Count);
            Assert.AreEqual("Message1", consoleMessages[0].Message);
            Assert.AreEqual("Message2", consoleMessages[1].Message);
            Assert.AreEqual("Message3", consoleMessages[2].Message);
        }
    }
}