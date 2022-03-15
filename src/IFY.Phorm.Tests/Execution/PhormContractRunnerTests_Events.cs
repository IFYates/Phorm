using IFY.Phorm.Data;
using IFY.Phorm.EventArgs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IFY.Phorm.Tests
{
    [TestClass]
    public class PhormContractRunnerTests_Events
    {
        private int _unwantedInvocations = 0;
        private Action<object?, CommandExecutingEventArgs>? _globalCommandExecuting = null;
        private void invokeHandler(object? sender, CommandExecutingEventArgs args) => _globalCommandExecuting?.Invoke(sender, args);
        private Action<object?, CommandExecutedEventArgs>? _globalCommandExecuted = null;
        private void invokeHandler(object? sender, CommandExecutedEventArgs args) => _globalCommandExecuted?.Invoke(sender, args);
        private Action<object?, UnexpectedRecordColumnEventArgs>? _globalUnexpectedRecordColumn = null;
        private void invokeHandler(object? sender, UnexpectedRecordColumnEventArgs args) => _globalUnexpectedRecordColumn?.Invoke(sender, args);

        [TestInitialize]
        public void Init()
        {
            Events.CommandExecuting += invokeHandler;
            Events.CommandExecuted += invokeHandler;
            Events.UnexpectedRecordColumn += invokeHandler;
        }
        [TestCleanup]
        public void Clean()
        {
            Events.CommandExecuting -= invokeHandler;
            Events.CommandExecuted -= invokeHandler;
            Events.UnexpectedRecordColumn -= invokeHandler;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private void eventInvokeFail(object? sender, System.EventArgs args)
        {
            ++_unwantedInvocations;
            Assert.Fail();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "Instance")]
        [DataRow(true , DisplayName = "Global")]
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

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
            Assert.AreEqual("[schema].[CallTest]", instanceEvent.Value.args.CommandText);
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

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
            Assert.AreEqual("[schema].[CallTest]", instanceEvent.Value.args.CommandText);
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

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
            Assert.AreEqual("[schema].[CallTest]", instanceEvent.Value.args.CommandText);
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

            var cmd = new TestDbCommand(new TestDbReader
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

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
            Assert.AreEqual("[schema].[CallTest]", instanceEvent.Value.args.CommandText);
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

            var cmd = new TestDbCommand(new TestDbReader
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

            var phorm = new TestPhormSession(new TestPhormConnectionProvider((s) => conn));

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
            var res = runner.Get<object[]>()!;

            // Assert
            Assert.AreSame(phorm, instanceEvent!.Value.sender);
            Assert.AreEqual(commandGuid, instanceEvent.Value.args.CommandGuid);
            Assert.AreEqual(typeof(object), instanceEvent.Value.args.EntityType);
            Assert.AreEqual("Column", instanceEvent.Value.args.ColumnName);
            Assert.AreSame(phorm, globalEvent!.Value.sender);
            Assert.AreSame(instanceEvent.Value.args, globalEvent!.Value.args);
        }
    }
}