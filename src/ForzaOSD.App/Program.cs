using ForzaOSD.Core;

namespace ForzaOSD.App;

internal static class Program
{
    private const string InstanceMutexName =
        @"Local\ForzaOSD.SingleInstance.86D0C06B-AC56-4D35-9CD1-7406D93D51B5";

    [STAThread]
    private static async Task Main()
    {
        using var instanceMutex = new Mutex(false, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "ForzaOSD is already running.",
                "ForzaOSD",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        var appDirectory = AppContext.BaseDirectory;
        Environment.CurrentDirectory = appDirectory;
        var configPath = Path.Combine(appDirectory, "config.json");
        var loaded = AppConfig.Load(configPath);
        await using var telemetry = new TelemetryService();
        string status = loaded.Warning;
        try
        {
            await telemetry.StartAsync(loaded.Config);
        }
        catch (Exception e)
        {
            status = string.IsNullOrEmpty(status) ? e.Message : status + "\n" + e.Message;
        }
        using var audio = new AudioSpectrumService();
        audio.Start(loaded.Config.Audio);
        using var host = new OverlayHost(loaded.Config, configPath, telemetry, audio, status);
        host.Run();
    }
}
