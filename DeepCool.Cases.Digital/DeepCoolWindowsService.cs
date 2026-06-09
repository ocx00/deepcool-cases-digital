using System.ServiceProcess;
using DeepCool.Cases.Digital.Core;

namespace DeepCool.Cases.Digital;

internal sealed class DeepCoolWindowsService : ServiceBase
{
    private readonly CancellationTokenSource _stopping = new();
    private Task? _loop;

    public DeepCoolWindowsService()
    {
        ServiceName = ServiceConstants.ServiceName;
        CanStop = true;
        CanPauseAndContinue = false;
        AutoLog = true;
    }

    protected override void OnStart(string[] args)
    {
        _loop = Task.Run(() => RunLoopAsync(_stopping.Token));
    }

    protected override void OnStop()
    {
        _stopping.Cancel();
        _loop?.Wait(TimeSpan.FromSeconds(5));
    }

    private static async Task RunLoopAsync(CancellationToken stoppingToken)
    {
        ServiceConfigurationStore configurationStore = new();
        using HardwareTelemetryService telemetry = new();
        using DeepCoolDisplayDevice display = new();

        telemetry.Open();
        int pageIndex = 0;
        int refreshesOnPage = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            ServiceConfiguration configuration = configurationStore.Load();
            IReadOnlyList<DisplayPage> pages = configuration.EnabledPages();
            DisplayPage page = pages[pageIndex % pages.Count];

            if (!display.TryConnect(configuration.CaseModel))
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                continue;
            }

            TelemetrySnapshot cpu = telemetry.ReadCpu();
            TelemetrySnapshot gpu = telemetry.ReadGpu();
            display.Write(configuration.CaseModel, page, cpu, gpu);

            refreshesOnPage++;
            if (refreshesOnPage >= configuration.SwitchCount())
            {
                refreshesOnPage = 0;
                pageIndex++;
            }

            await Task.Delay(configuration.RefreshInterval(), stoppingToken);
        }
    }
}
