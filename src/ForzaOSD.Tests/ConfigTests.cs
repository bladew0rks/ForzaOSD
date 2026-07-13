using ForzaOSD.Core;

namespace ForzaOSD.Tests;

public sealed class ConfigTests
{
    [Fact]
    public void ConfigRoundTrips()
    {
        Assert.Equal("forzaosd.vfd", new AppConfig().HudProfile);

        var path = Path.GetTempFileName();
        try
        {
            var source = new AppConfig
            {
                UdpPort = 7777,
                Metric = false,
                HotkeyVk = 0x41,
                HotkeyModifiers = 6,
            };
            source.Save(path);
            var loaded = AppConfig.Load(path).Config;
            Assert.Equal(7777, loaded.UdpPort);
            Assert.False(loaded.Metric);
            Assert.Equal(0x41, loaded.HotkeyVk);
            Assert.Equal(6, loaded.HotkeyModifiers);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void VersionFourMigratesToShiftEscape()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"version\":4,\"hotkey_vk\":121}");
            var loaded = AppConfig.Load(path).Config;
            Assert.Equal(8, loaded.Version);
            Assert.Equal(0x1B, loaded.HotkeyVk);
            Assert.Equal(4, loaded.HotkeyModifiers);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AudioSettingsRoundTripAndNormalize()
    {
        var path = Path.GetTempFileName();
        try
        {
            var source = new AppConfig
            {
                Audio = new()
                {
                    Enabled = false,
                    CaptureMode = "application",
                    OutputDeviceId = "device-id",
                    ApplicationName = "spotify",
                    MediaControlsEnabled = false,
                    GamepadIndex = 2,
                },
                HudModules = ["forzaosd.vfd_radio", "forzaosd.vfd_radio", ""],
            };
            source.Save(path);
            var loaded = AppConfig.Load(path).Config;
            Assert.False(loaded.Audio.Enabled);
            Assert.Equal("application", loaded.Audio.CaptureMode);
            Assert.Equal("device-id", loaded.Audio.OutputDeviceId);
            Assert.Equal("spotify", loaded.Audio.ApplicationName);
            Assert.False(loaded.Audio.MediaControlsEnabled);
            Assert.Equal(2, loaded.Audio.GamepadIndex);
            Assert.Equal(["forzaosd.vfd_radio"], loaded.HudModules);

            loaded.Audio.CaptureMode = "invalid";
            loaded.Audio.GamepadIndex = 99;
            loaded.Save(path);
            var normalized = AppConfig.Load(path).Config;
            Assert.Equal("output", normalized.Audio.CaptureMode);
            Assert.Equal(3, normalized.Audio.GamepadIndex);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void VisibilityMatchesFreshRaceState()
    {
        var empty = new TelemetrySnapshot();
        Assert.False(empty.IsDriving);
        Assert.False(empty.ShouldShow(false));
        Assert.True(empty.ShouldShow(true));
        var racing = empty with
        {
            HasFrame = true,
            Stale = false,
            Frame = new() { RaceOn = true },
        };
        Assert.True(racing.IsDriving);
        Assert.True(racing.ShouldShow(false));
        var stale = racing with { Stale = true };
        Assert.False(stale.IsDriving);
        Assert.False(stale.ShouldShow(false));

        var menu = racing with { Frame = racing.Frame with { RaceOn = false } };
        Assert.False(menu.IsDriving);
        Assert.False(menu.ShouldShow(false));
    }
}
