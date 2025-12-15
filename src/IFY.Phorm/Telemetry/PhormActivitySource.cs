using System.Diagnostics;

namespace IFY.Phorm.Telemetry;

internal static class PhormActivitySource
{
    public const string SourceName = "IFY.Phorm";
    public const string Version = "2.1.0"; // Keep in sync with package version

    public static readonly ActivitySource Source = new(SourceName, Version);
}