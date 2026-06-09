using HidSharp;

namespace DeepCool.Cases.Digital.Core;

public sealed class DeepCoolDisplayDevice : IDisposable
{
    private HidStream? _stream;
    private CaseModel? _connectedModel;
    private bool _chSeriesInitialized;

    public bool TryConnect(CaseModel model)
    {
        if (_stream is not null && _connectedModel == model)
        {
            return true;
        }

        Dispose();

        CaseModelInfo info = CaseModelInfo.Get(model);
        HidDevice? device = info.Devices
            .SelectMany(deviceId => DeviceList.Local.GetHidDevices(deviceId.VendorId, deviceId.ProductId))
            .FirstOrDefault();

        if (device is null)
        {
            return false;
        }

        if (!device.TryOpen(out HidStream? stream))
        {
            return false;
        }

        stream.WriteTimeout = 1000;
        _stream = stream;
        _connectedModel = model;
        _chSeriesInitialized = false;
        return true;
    }

    public void Write(CaseModel model, DisplayPage page, TelemetrySnapshot cpu, TelemetrySnapshot gpu)
    {
        if (_stream is null)
        {
            return;
        }

        CaseProtocol protocol = CaseModelInfo.Get(model).Protocol;
        byte[] packet = protocol switch
        {
            CaseProtocol.ChSeries => CreateChSeriesPacket(page, cpu, gpu),
            CaseProtocol.ChSeriesGen2 => CreateChSeriesGen2Packet(page, cpu, gpu),
            CaseProtocol.Ch510 => CreateCh510Packet(page, cpu, gpu),
            _ => []
        };

        Write(packet);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
        _connectedModel = null;
        _chSeriesInitialized = false;
    }

    private void Write(byte[] packet)
    {
        try
        {
            _stream?.Write(packet);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            _ = ex;
            Dispose();
        }
    }

    private byte[] CreateChSeriesPacket(DisplayPage page, TelemetrySnapshot cpu, TelemetrySnapshot gpu)
    {
        if (!_chSeriesInitialized)
        {
            byte[] init = new byte[64];
            init[0] = 16;
            init[1] = 170;
            Write(init);
            _chSeriesInitialized = true;
        }

        byte[] data = new byte[64];
        data[0] = 16;

        TelemetrySnapshot primary = page == DisplayPage.Gpu ? gpu : cpu;
        TelemetrySnapshot secondary = page == DisplayPage.Gpu ? cpu : gpu;

        bool usagePage = page == DisplayPage.Gpu;
        WriteThreeDigits(data, 3, usagePage ? primary.UsagePercent : (int)MathF.Round(primary.TemperatureCelsius));
        WriteThreeDigits(data, 8, usagePage ? secondary.UsagePercent : (int)MathF.Round(secondary.TemperatureCelsius));

        data[1] = usagePage ? (byte)76 : (byte)19;
        data[6] = data[1];
        data[2] = UsageBar(primary.UsagePercent);
        data[7] = UsageBar(secondary.UsagePercent);

        return data;
    }

    private static byte[] CreateChSeriesGen2Packet(DisplayPage page, TelemetrySnapshot cpu, TelemetrySnapshot gpu)
    {
        byte[] data = new byte[64];

        data[0] = 16;
        data[1] = 104;
        data[2] = 1;
        data[3] = 6;
        data[4] = 35;
        data[5] = 1;
        data[6] = page switch
        {
            DisplayPage.Gpu => (byte)4,
            _ => (byte)2
        };

        if (page == DisplayPage.Gpu)
        {
            WriteUInt16BigEndian(data, 19, gpu.PowerWatts);
            WriteSingleBigEndian(data, 21, gpu.TemperatureCelsius);
            data[25] = gpu.UsagePercent;
            WriteUInt16BigEndian(data, 26, gpu.FrequencyMhz);
        }
        else if (page == DisplayPage.Cpu)
        {
            WriteUInt16BigEndian(data, 7, cpu.PowerWatts);
            WriteSingleBigEndian(data, 10, cpu.TemperatureCelsius);
            data[14] = cpu.UsagePercent;
            WriteUInt16BigEndian(data, 15, cpu.FrequencyMhz);
        }

        int checksum = 0;
        for (int i = 1; i <= 39; i++)
        {
            checksum += data[i];
        }

        data[40] = (byte)(checksum % 256);
        data[41] = 22;

        return data;
    }

    private static byte[] CreateCh510Packet(DisplayPage page, TelemetrySnapshot cpu, TelemetrySnapshot gpu)
    {
        TelemetrySnapshot telemetry = page == DisplayPage.Gpu ? gpu : cpu;
        string message = $"HLXDATA({telemetry.UsagePercent},{(int)MathF.Round(telemetry.TemperatureCelsius)},0,0,C)\r\n";
        return System.Text.Encoding.ASCII.GetBytes(message);
    }

    private static byte UsageBar(byte usage)
    {
        return usage < 15 ? (byte)1 : (byte)Math.Clamp((int)MathF.Round(usage / 10.0f), 1, 10);
    }

    private static void WriteThreeDigits(byte[] data, int offset, int value)
    {
        int clamped = Math.Clamp(value, 0, 999);
        data[offset] = (byte)(clamped / 100);
        data[offset + 1] = (byte)(clamped % 100 / 10);
        data[offset + 2] = (byte)(clamped % 10);
    }

    private static void WriteUInt16BigEndian(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteSingleBigEndian(byte[] data, int offset, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        Buffer.BlockCopy(bytes, 0, data, offset, bytes.Length);
    }
}
