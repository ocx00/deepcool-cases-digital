using System.Text.Json.Serialization;

namespace DeepCool.Cases.Digital.Core;

public sealed class ServiceConfiguration
{
    public CaseModel CaseModel { get; set; } = CaseModel.Ch270;
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public int RefreshSeconds { get; set; } = 3;
    public int SwitchAfterRefreshes { get; set; } = 3;

    [JsonIgnore]
    public string CaseDisplayName => CaseModelInfo.Get(CaseModel).DisplayName;

    public IReadOnlyList<DisplayPage> EnabledPages()
    {
        List<DisplayPage> pages = [];

        if (ShowCpu)
        {
            pages.Add(DisplayPage.Cpu);
        }

        if (ShowGpu)
        {
            pages.Add(DisplayPage.Gpu);
        }

        if (pages.Count == 0)
        {
            pages.Add(DisplayPage.Cpu);
        }

        return pages;
    }

    public TimeSpan RefreshInterval()
    {
        return TimeSpan.FromSeconds(RefreshSeconds is 2 or 3 or 5 or 10 ? RefreshSeconds : 3);
    }

    public int SwitchCount()
    {
        return SwitchAfterRefreshes is >= 1 and <= 10 ? SwitchAfterRefreshes : 3;
    }
}

public enum DisplayPage
{
    Cpu,
    Gpu
}

public enum CaseProtocol
{
    ChSeries,
    ChSeriesGen2,
    Ch510
}

public enum CaseModel
{
    Ch170,
    Ch270,
    Ch360,
    Ch510Mesh,
    Ch560,
    Ch690,
    Morpheus
}

public sealed record CaseModelInfo(CaseModel Model, string DisplayName, CaseProtocol Protocol, IReadOnlyList<DeviceId> Devices)
{
    public static IReadOnlyList<CaseModelInfo> All { get; } =
    [
        new(CaseModel.Ch170, "CH170 DIGITAL", CaseProtocol.ChSeriesGen2, [new(0x3633, 19)]),
        new(CaseModel.Ch270, "CH270 DIGITAL", CaseProtocol.ChSeriesGen2, [new(0x3633, 22)]),
        new(CaseModel.Ch360, "CH360 DIGITAL", CaseProtocol.ChSeries, [new(0x3633, 21)]),
        new(CaseModel.Ch510Mesh, "CH510 MESH DIGITAL", CaseProtocol.Ch510, [new(0x34D3, 0x1100)]),
        new(CaseModel.Ch560, "CH560 DIGITAL", CaseProtocol.ChSeries, [new(0x3633, 5)]),
        new(CaseModel.Ch690, "CH690 DIGITAL", CaseProtocol.ChSeriesGen2, [new(0x3633, 27)]),
        new(CaseModel.Morpheus, "MORPHEUS", CaseProtocol.ChSeries, [new(0x3633, 7)])
    ];

    public static CaseModelInfo Get(CaseModel model)
    {
        return All.First(item => item.Model == model);
    }
}

public readonly record struct DeviceId(int VendorId, int ProductId);
