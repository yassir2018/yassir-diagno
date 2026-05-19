namespace YassirDiagno.Models;

public enum SensorType
{
    Temperature,
    Load,
    Power,
    Clock,
    Voltage,
    Fan,
    Storage,
    Battery,
    Other
}

public enum HealthLevel
{
    Excellent,
    Good,
    Warning,
    Critical
}

public record SensorReading(
    string Id,
    string Name,
    SensorType Type,
    double Value,
    string Unit,
    DateTime Timestamp,
    double? Min = null,
    double? Max = null,
    HealthLevel Health = HealthLevel.Good);
