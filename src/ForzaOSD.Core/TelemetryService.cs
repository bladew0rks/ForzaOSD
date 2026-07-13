using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ForzaOSD.Core;

public sealed class TelemetryService : IAsyncDisposable
{
    private readonly Lock gate = new();
    private TelemetrySnapshot snapshot = new();
    private CancellationTokenSource? cancellation;
    private Task? receiver;
    private bool capturedUnknown;

    public TelemetrySnapshot Snapshot
    {
        get
        {
            lock (gate)
            {
                var copy = snapshot;
                if (!copy.HasFrame)
                    return copy;
                var stale =
                    Stopwatch.GetElapsedTime(copy.Frame.ReceivedTimestamp)
                    > TimeSpan.FromMilliseconds(500);
                return copy with { Stale = stale };
            }
        }
    }

    public async Task StartAsync(AppConfig config)
    {
        await StopAsync();
        cancellation = new();
        capturedUnknown = false;
        receiver = ReceiveAsync(config, cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (cancellation is null)
            return;
        await cancellation.CancelAsync();
        if (receiver is not null)
            try
            {
                await receiver;
            }
            catch (OperationCanceledException) { }
        cancellation.Dispose();
        cancellation = null;
        receiver = null;
    }

    private async Task ReceiveAsync(AppConfig config, CancellationToken token)
    {
        using var udp = new UdpClient(
            new IPEndPoint(IPAddress.Parse(config.BindAddress), config.UdpPort)
        );
        lock (gate)
            snapshot = new() { Format = $"Listening on {config.BindAddress}:{config.UdpPort}" };
        var rateAt = Stopwatch.GetTimestamp();
        ulong ratePackets = 0;
        while (!token.IsCancellationRequested)
        {
            var received = await udp.ReceiveAsync(token);
            ratePackets++;
            var result = ForzaDashDecoder.Decode(received.Buffer);
            if (
                result.Status == DecodeStatus.UnsupportedSize
                && config.CaptureUnknownPackets
                && !capturedUnknown
            )
            {
                Directory.CreateDirectory("captures");
                var name =
                    $"unknown-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{received.Buffer.Length}.bin";
                await File.WriteAllBytesAsync(
                    Path.Combine("captures", name),
                    received.Buffer,
                    token
                );
                capturedUnknown = true;
            }
            lock (gate)
            {
                var elapsed = Stopwatch.GetElapsedTime(rateAt).TotalSeconds;
                var pps = snapshot.PacketsPerSecond;
                if (elapsed >= 1)
                {
                    pps = ratePackets / elapsed;
                    ratePackets = 0;
                    rateAt = Stopwatch.GetTimestamp();
                }
                snapshot =
                    result.Status == DecodeStatus.Decoded
                        ? snapshot with
                        {
                            Frame = result.Frame,
                            HasFrame = true,
                            Stale = false,
                            PacketsReceived = snapshot.PacketsReceived + 1,
                            LastPacketSize = received.Buffer.Length,
                            PacketsPerSecond = pps,
                            Format = result.Format,
                            Detail = "",
                        }
                        : snapshot with
                        {
                            PacketsReceived = snapshot.PacketsReceived + 1,
                            MalformedPackets = snapshot.MalformedPackets + 1,
                            LastPacketSize = received.Buffer.Length,
                            PacketsPerSecond = pps,
                            Format = result.Format,
                            Detail = result.Detail,
                        };
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
