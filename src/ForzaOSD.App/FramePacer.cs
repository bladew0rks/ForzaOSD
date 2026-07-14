using System.Diagnostics;

namespace ForzaOSD.App;

internal sealed class FramePacer
{
    private long previousFrame;
    private int frameRate;

    internal void Wait(int maxFps)
    {
        var now = Stopwatch.GetTimestamp();
        if (previousFrame == 0 || frameRate != maxFps)
        {
            previousFrame = now;
            frameRate = maxFps;
            return;
        }

        var interval = Math.Max(1, Stopwatch.Frequency / maxFps);
        var remaining = previousFrame + interval - now;
        if (remaining > 0)
        {
            var delay = (int)Math.Ceiling(remaining * 1000d / Stopwatch.Frequency);
            Thread.Sleep(delay);
        }
        previousFrame = Stopwatch.GetTimestamp();
    }

    internal void Reset()
    {
        previousFrame = 0;
    }
}
