using LibreHardwareMonitor.Hardware;

namespace DeepCool.Cases.Digital.Core;

public sealed class HardwareTelemetryService : IDisposable
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true
    };

    private bool _opened;

    public void Open()
    {
        if (_opened)
        {
            return;
        }

        _computer.Open();
        _opened = true;
    }

    public TelemetrySnapshot ReadCpu()
    {
        IHardware? cpu = FindHardware(HardwareType.Cpu);
        return cpu is null
            ? new TelemetrySnapshot(0, 0, 0, 0)
            : ReadHardware(cpu, "CPU Total", "CPU Package", "CPU Package", "Core");
    }

    public TelemetrySnapshot ReadGpu()
    {
        IHardware? gpu = _computer.Hardware.FirstOrDefault(static hardware =>
            hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia);

        return gpu is null
            ? new TelemetrySnapshot(0, 0, 0, 0)
            : ReadHardware(gpu, "GPU Core", "GPU Core", "GPU Package", "GPU Core");
    }

    public void Dispose()
    {
        _computer.Close();
    }

    private IHardware? FindHardware(HardwareType type)
    {
        return _computer.Hardware.FirstOrDefault(hardware => hardware.HardwareType == type);
    }

    private static TelemetrySnapshot ReadHardware(
        IHardware hardware,
        string loadName,
        string temperatureName,
        string powerName,
        string clockName)
    {
        hardware.Update();

        SensorValue sensors = new(hardware.Sensors);

        float temperature = sensors.Find(SensorType.Temperature, temperatureName)
            ?? sensors.First(SensorType.Temperature)
            ?? 0;

        float load = sensors.Find(SensorType.Load, loadName)
            ?? sensors.First(SensorType.Load)
            ?? 0;

        float power = sensors.Find(SensorType.Power, powerName)
            ?? sensors.First(SensorType.Power)
            ?? 0;

        float clock = sensors.Find(SensorType.Clock, clockName)
            ?? sensors.First(SensorType.Clock)
            ?? 0;

        return new TelemetrySnapshot(
            ClampUShort(power),
            temperature,
            ClampByte(load),
            ClampUShort(clock));
    }

    private static ushort ClampUShort(float value)
    {
        if (float.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return (ushort)Math.Clamp((int)MathF.Round(value), 0, ushort.MaxValue);
    }

    private static byte ClampByte(float value)
    {
        if (float.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return (byte)Math.Clamp((int)MathF.Round(value), 0, 100);
    }

    private readonly struct SensorValue
    {
        private readonly IReadOnlyList<ISensor> _sensors;

        public SensorValue(IReadOnlyList<ISensor> sensors)
        {
            _sensors = sensors;
        }

        public float? Find(SensorType type, string name)
        {
            return _sensors
                .Where(sensor => sensor.SensorType == type && sensor.Value.HasValue)
                .FirstOrDefault(sensor => sensor.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                ?.Value;
        }

        public float? First(SensorType type)
        {
            return _sensors
                .FirstOrDefault(sensor => sensor.SensorType == type && sensor.Value.HasValue)
                ?.Value;
        }
    }
}
