namespace DeepCool.Cases.Digital.Core;

public sealed record TelemetrySnapshot(
    ushort PowerWatts,
    float TemperatureCelsius,
    byte UsagePercent,
    ushort FrequencyMhz);
