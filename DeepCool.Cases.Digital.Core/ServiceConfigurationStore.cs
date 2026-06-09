using System.Text.Json;
using Microsoft.Win32;

namespace DeepCool.Cases.Digital.Core;

public sealed class ServiceConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public ServiceConfigurationStore()
    {
        _path = Path.Combine(AppContext.BaseDirectory, ServiceConstants.ConfigurationFileName);
    }

    public ServiceConfiguration Load()
    {
        ServiceConfiguration? registryConfiguration = LoadFromRegistry();
        if (registryConfiguration is not null)
        {
            return registryConfiguration;
        }

        try
        {
            if (!File.Exists(_path))
            {
                ServiceConfiguration defaultConfiguration = new();
                Save(defaultConfiguration);
                return defaultConfiguration;
            }

            ServiceConfiguration? configuration = JsonSerializer.Deserialize<ServiceConfiguration>(File.ReadAllText(_path), JsonOptions);
            return configuration ?? new ServiceConfiguration();
        }
        catch (Exception ex)
        {
            _ = ex;
            return new ServiceConfiguration();
        }
    }

    public void Save(ServiceConfiguration configuration)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(configuration, JsonOptions));
    }

    private static ServiceConfiguration? LoadFromRegistry()
    {
        using RegistryKey? key = OpenConfigurationKey();
        if (key is null)
        {
            return null;
        }

        return new ServiceConfiguration
        {
            CaseModel = ParseCaseModel(ReadString(key, "CaseModel"), CaseModel.Ch270),
            ShowCpu = ReadBool(key, "ShowCpu", defaultValue: true),
            ShowGpu = ReadBool(key, "ShowGpu", defaultValue: true),
            RefreshSeconds = ReadInt(key, "RefreshSeconds", defaultValue: 3),
            SwitchAfterRefreshes = ReadInt(key, "SwitchAfterRefreshes", defaultValue: 3)
        };
    }

    private static string? ReadString(RegistryKey key, string name)
    {
        return key.GetValue(name)?.ToString();
    }

    private static int ReadInt(RegistryKey key, string name, int defaultValue)
    {
        object? value = key.GetValue(name);
        if (value is int intValue)
        {
            return intValue;
        }

        string? text = value?.ToString();
        if (text?.StartsWith('#') == true)
        {
            text = text[1..];
        }

        if (int.TryParse(text, out int parsed))
        {
            return parsed;
        }

        string digits = new((text ?? string.Empty).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out parsed) ? parsed : defaultValue;
    }

    private static bool ReadBool(RegistryKey key, string name, bool defaultValue)
    {
        return ReadInt(key, name, defaultValue ? 1 : 0) != 0;
    }

    private static CaseModel ParseCaseModel(string? value, CaseModel defaultValue)
    {
        return Enum.TryParse(value, ignoreCase: true, out CaseModel model) ? model : defaultValue;
    }

    private static RegistryKey? OpenConfigurationKey()
    {
        RegistryKey? key = RegistryKey
            .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(@"Software\DeepCool Cases Digital");

        if (key is not null)
        {
            return key;
        }

        return RegistryKey
            .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
            .OpenSubKey(@"Software\DeepCool Cases Digital");
    }
}
