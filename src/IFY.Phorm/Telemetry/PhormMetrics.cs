using System.Diagnostics.Metrics;

namespace IFY.Phorm.Telemetry;

internal static class PhormMetrics
{
    public const string MeterName = "IFY.Phorm";
    
    public static readonly Meter Meter = new(MeterName, PhormActivitySource.Version);
    
    public static readonly Counter<long> CallCounter = Meter.CreateCounter<long>(
        "phorm.calls",
        description: "Number of Phorm contract calls");
    
    public static readonly Counter<long> GetCounter = Meter.CreateCounter<long>(
        "phorm.gets",
        description: "Number of Phorm get operations");
    
    public static readonly Histogram<double> CallDuration = Meter.CreateHistogram<double>(
        "phorm.call.duration",
        unit: "ms",
        description: "Duration of Phorm calls");
    
    public static readonly Histogram<double> GetDuration = Meter.CreateHistogram<double>(
        "phorm.get.duration",
        unit: "ms",
        description: "Duration of Phorm get operations");
    
    public static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
        "phorm.errors",
        description: "Number of errors encountered");
}