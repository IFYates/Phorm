using IFY.Phorm;
using IFY.Phorm.Data;
using IFY.Phorm.Execution;
using IFY.Phorm.Tests;
using System.Diagnostics;

[TestClass]
public sealed class TelemetryTests
{
    public TestContext TestContext { get; set; }

    private readonly List<Activity> _activities = [];
    
    [TestInitialize]
    public void Setup()
    {
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = source => source.Name == "IFY.Phorm",
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _activities.Add(activity)
        });
    }
    
    [TestMethod]
    public async Task CallAsync_Creates_Activity()
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
        Assert.IsTrue(_activities.Any(a => a.DisplayName == "phorm.call"));
        var activity = _activities.First(a => a.DisplayName == "phorm.call");
        Assert.IsNotNull(activity.GetTagItem("db.statement"));
    }
}