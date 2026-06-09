using DeepCool.Cases.Digital.Core;

namespace DeepCool.Cases.Digital;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        RunService(args);
    }

    private static void RunService(string[] args)
    {
        System.ServiceProcess.ServiceBase.Run(new DeepCoolWindowsService());
    }
}
