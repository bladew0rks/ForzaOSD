using System.Diagnostics;
using System.Runtime.InteropServices;
using ForzaOSD.Core;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using Windows.Media.Control;

namespace ForzaOSD.App;

internal sealed class AudioSpectrumService : IDisposable
{
    private const int BandCount = 28;
    private readonly object gate = new();
    private readonly object analyzerGate = new();
    private readonly SpectrumAnalyzer analyzer = new(BandCount);
    private readonly CancellationTokenSource metadataCancellation = new();
    private readonly SemaphoreSlim mediaCommandGate = new(1, 1);
    private readonly GamepadMediaControls gamepad = new();
    private WasapiLoopbackCapture? outputCapture;
    private MMDevice? outputDevice;
    private ProcessLoopbackCapture? applicationCapture;
    private Task? metadataTask;
    private GlobalSystemMediaTransportControlsSessionManager? mediaManager;
    private AudioSnapshot snapshot = AudioSnapshot.Empty(BandCount);
    private string captureStatus = "Audio capture is starting";
    private string metadataStatus = "";
    private string title = "";
    private string artist = "";
    private string source = "";
    private bool playing;
    private bool enabled;
    private string targetApplication = "";
    private string mediaControlStatus = "";
    private IReadOnlyList<AudioOption> outputOptions =
        new[] { new AudioOption("", "Default Windows output") };
    private IReadOnlyList<AudioOption> applicationOptions = Array.Empty<AudioOption>();
    private long outputOptionsUpdated;
    private long applicationOptionsUpdated;
    private long captureGeneration;

    internal AudioSnapshot Snapshot
    {
        get
        {
            lock (gate)
                return snapshot;
        }
    }

    internal string MediaControlStatus
    {
        get
        {
            lock (gate)
                return string.IsNullOrEmpty(mediaControlStatus)
                    ? gamepad.Status
                    : gamepad.Status + " - " + mediaControlStatus;
        }
    }

    internal void PollMediaControls(AudioConfig config, bool isDriving)
    {
        if (!isDriving)
            SetMediaControlStatus("");

        var command = gamepad.Poll(config, isDriving);
        if (command is not null)
            _ = ExecuteMediaCommandAsync(command.Value, metadataCancellation.Token);
    }

    internal IReadOnlyList<AudioOption> GetOutputOptions()
    {
        var now = Environment.TickCount64;
        if (now - outputOptionsUpdated < 2000)
            return outputOptions;
        var options = new List<AudioOption> { new("", "Default Windows output") };
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (
                var device in enumerator.EnumerateAudioEndPoints(
                    DataFlow.Render,
                    DeviceState.Active
                )
            )
            {
                using (device)
                    options.Add(new(device.ID, device.FriendlyName));
            }
        }
        catch
        {
            // The default entry remains usable even if enumeration races a device change.
        }
        outputOptions = options;
        outputOptionsUpdated = now;
        return outputOptions;
    }

    internal IReadOnlyList<AudioOption> GetApplicationOptions()
    {
        var now = Environment.TickCount64;
        if (now - applicationOptionsUpdated < 2000)
            return applicationOptions;
        var names = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == Environment.ProcessId || process.MainWindowHandle == 0)
                        continue;
                    var name = process.ProcessName;
                    var label = string.IsNullOrWhiteSpace(process.MainWindowTitle)
                        ? name
                        : $"{name} - {process.MainWindowTitle}";
                    names.TryAdd(name, label);
                }
                catch
                {
                    // Processes can exit or deny access while the list is being built.
                }
            }
        }
        applicationOptions = names
            .Select(pair => new AudioOption(pair.Key, pair.Value))
            .ToArray();
        applicationOptionsUpdated = now;
        return applicationOptions;
    }

    internal void Start(AudioConfig config)
    {
        var generation = Interlocked.Increment(ref captureGeneration);
        StopCapture();
        enabled = config.Enabled;
        targetApplication = config.CaptureMode == "application" ? config.ApplicationName : "";
        EnsureMetadataLoop();

        lock (analyzerGate)
            analyzer.Reset();
        lock (gate)
        {
            snapshot = AudioSnapshot.Empty(BandCount);
            captureStatus = enabled ? "Audio capture is starting" : "Audio capture disabled";
            PublishLocked();
        }

        if (!enabled)
            return;

        if (config.CaptureMode == "application" && !string.IsNullOrWhiteSpace(targetApplication))
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348))
            {
                lock (gate)
                {
                    captureStatus =
                        "Application capture requires Windows build 20348; using the selected output mix";
                    PublishLocked();
                }
                StartOutputCapture(config.OutputDeviceId);
                return;
            }

            var process = FindApplicationProcess(targetApplication);
            if (process is not null)
            {
                using (process)
                    StartApplicationCapture(
                        process.Id,
                        process.ProcessName,
                        config.OutputDeviceId,
                        generation
                    );
                return;
            }

            lock (gate)
            {
                captureStatus = $"{targetApplication} is not running; using the selected output mix";
                PublishLocked();
            }
        }

        StartOutputCapture(config.OutputDeviceId);
    }

    private static Process? FindApplicationProcess(string name)
    {
        var normalized = Path.GetFileNameWithoutExtension(name.Trim());
        Process? oldest = null;
        DateTime oldestStart = DateTime.MaxValue;
        foreach (var process in Process.GetProcessesByName(normalized))
        {
            try
            {
                var started = process.StartTime;
                if (started >= oldestStart)
                {
                    process.Dispose();
                    continue;
                }
                oldest?.Dispose();
                oldest = process;
                oldestStart = started;
            }
            catch
            {
                process.Dispose();
            }
        }
        return oldest;
    }

    private void StartApplicationCapture(
        int processId,
        string processName,
        string fallbackDeviceId,
        long generation
    )
    {
        applicationCapture = new ProcessLoopbackCapture(
            processId,
            ProcessSamples,
            () =>
            {
                if (generation != Interlocked.Read(ref captureGeneration))
                    return;
                lock (gate)
                {
                    captureStatus = $"Analyzing {processName} (application loopback)";
                    PublishLocked();
                }
            },
            error =>
            {
                if (generation != Interlocked.Read(ref captureGeneration))
                    return;
                lock (gate)
                {
                    captureStatus = $"Application capture unavailable ({error}); using output mix";
                    PublishLocked();
                }
                if (enabled && generation == Interlocked.Read(ref captureGeneration))
                    StartOutputCapture(fallbackDeviceId);
            }
        );
    }

    private void StartOutputCapture(string deviceId)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            outputDevice = string.IsNullOrWhiteSpace(deviceId)
                ? enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                : enumerator.GetDevice(deviceId);
            outputCapture = new WasapiLoopbackCapture(outputDevice);
            outputCapture.DataAvailable += OnOutputData;
            outputCapture.RecordingStopped += OnOutputStopped;
            outputCapture.StartRecording();
            lock (gate)
            {
                captureStatus = $"Analyzing output: {outputDevice.FriendlyName}";
                PublishLocked();
            }
        }
        catch (Exception exception)
        {
            outputCapture?.Dispose();
            outputCapture = null;
            outputDevice?.Dispose();
            outputDevice = null;
            lock (gate)
            {
                captureStatus = "Audio capture failed: " + exception.Message;
                PublishLocked();
            }
        }
    }

    private void OnOutputData(object? sender, WaveInEventArgs args)
    {
        var capture = outputCapture;
        if (capture is null)
            return;
        SpectrumFrame? frame;
        lock (analyzerGate)
            frame = analyzer.AddPcm(
                args.Buffer.AsSpan(0, args.BytesRecorded),
                capture.WaveFormat
            );
        if (frame is not null)
            PublishSpectrum(frame.Value);
    }

    private void OnOutputStopped(object? sender, StoppedEventArgs args)
    {
        if (args.Exception is null || !enabled)
            return;
        lock (gate)
        {
            captureStatus = "Output capture stopped: " + args.Exception.Message;
            PublishLocked();
        }
    }

    private unsafe void ProcessSamples(nint data, uint frames, bool silent)
    {
        SpectrumFrame? frame;
        lock (analyzerGate)
        {
            if (silent || data == 0)
                frame = analyzer.AddSilence(checked((int)frames));
            else
            {
                var bytes = checked((int)frames * ProcessLoopbackCapture.BlockAlign);
                frame = analyzer.AddPcm16(
                    new ReadOnlySpan<byte>((void*)data, bytes),
                    ProcessLoopbackCapture.Channels
                );
            }
        }
        if (frame is not null)
            PublishSpectrum(frame.Value);
    }

    private void PublishSpectrum(SpectrumFrame frame)
    {
        lock (gate)
        {
            snapshot = snapshot with
            {
                Available = true,
                Rms = frame.Rms,
                Peak = frame.Peak,
                Bands = frame.Bands,
            };
        }
    }

    private void EnsureMetadataLoop()
    {
        if (metadataTask is not null)
            return;
        metadataTask = Task.Run(() => MetadataLoopAsync(metadataCancellation.Token));
    }

    private async Task MetadataLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            mediaManager = manager;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var selected = SelectMediaSession(manager);

                    if (selected is null)
                    {
                        UpdateMetadata("", "", "", false, "No media session is active");
                    }
                    else
                    {
                        var properties = await selected.TryGetMediaPropertiesAsync();
                        var playback = selected.GetPlaybackInfo();
                        var mediaArtist = properties?.Artist ?? "";
                        if (string.IsNullOrWhiteSpace(mediaArtist))
                            mediaArtist = properties?.AlbumArtist ?? "";
                        UpdateMetadata(
                            properties?.Title ?? "",
                            mediaArtist,
                            selected.SourceAppUserModelId,
                            playback.PlaybackStatus
                                == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                            ""
                        );
                    }
                }
                catch (Exception exception)
                {
                    UpdateMetadata("", "", "", false, "Metadata unavailable: " + exception.Message);
                }
                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            UpdateMetadata("", "", "", false, "Metadata unavailable: " + exception.Message);
        }
    }

    private GlobalSystemMediaTransportControlsSession? SelectMediaSession(
        GlobalSystemMediaTransportControlsSessionManager manager
    )
    {
        if (string.IsNullOrWhiteSpace(targetApplication))
            return manager.GetCurrentSession();
        return manager
                .GetSessions()
                .FirstOrDefault(session =>
                    session.SourceAppUserModelId.Contains(
                        targetApplication,
                        StringComparison.OrdinalIgnoreCase
                    )
                ) ?? manager.GetCurrentSession();
    }

    private async Task ExecuteMediaCommandAsync(
        MediaCommand command,
        CancellationToken cancellationToken
    )
    {
        var acquired = false;
        try
        {
            await mediaCommandGate.WaitAsync(cancellationToken);
            acquired = true;
            var manager = mediaManager;
            var session = manager is null ? null : SelectMediaSession(manager);
            if (session is null)
            {
                SetMediaControlStatus("No active media session");
                return;
            }

            var handled = command switch
            {
                MediaCommand.PreviousTrack => await session.TrySkipPreviousAsync(),
                MediaCommand.NextTrack => await session.TrySkipNextAsync(),
                _ => await session.TryTogglePlayPauseAsync(),
            };
            var action = command switch
            {
                MediaCommand.PreviousTrack => "Previous track",
                MediaCommand.NextTrack => "Next track",
                _ => "Play/pause",
            };
            SetMediaControlStatus(handled ? action : action + " unsupported by player");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            SetMediaControlStatus("Media command failed: " + exception.Message);
        }
        finally
        {
            if (acquired)
                mediaCommandGate.Release();
        }
    }

    private void SetMediaControlStatus(string status)
    {
        lock (gate)
            mediaControlStatus = status;
    }

    private void UpdateMetadata(
        string newTitle,
        string newArtist,
        string newSource,
        bool isPlaying,
        string status
    )
    {
        lock (gate)
        {
            title = newTitle;
            artist = newArtist;
            source = newSource;
            playing = isPlaying;
            metadataStatus = status;
            PublishLocked();
        }
    }

    private void PublishLocked()
    {
        snapshot = snapshot with
        {
            Enabled = enabled,
            Title = title,
            Artist = artist,
            Source = source,
            Playing = playing,
            Status = string.IsNullOrEmpty(metadataStatus)
                ? captureStatus
                : captureStatus + "\n" + metadataStatus,
        };
    }

    private void StopCapture()
    {
        enabled = false;
        var process = Interlocked.Exchange(ref applicationCapture, null);
        process?.Dispose();

        var output = Interlocked.Exchange(ref outputCapture, null);
        if (output is not null)
        {
            output.DataAvailable -= OnOutputData;
            output.RecordingStopped -= OnOutputStopped;
            try
            {
                output.StopRecording();
            }
            catch { }
            output.Dispose();
        }
        Interlocked.Exchange(ref outputDevice, null)?.Dispose();
    }

    public void Dispose()
    {
        Interlocked.Increment(ref captureGeneration);
        StopCapture();
        metadataCancellation.Cancel();
        try
        {
            metadataTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception)
            when (exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        { }
        metadataCancellation.Dispose();
    }
}

internal readonly record struct AudioOption(string Id, string Name);

internal readonly record struct AudioSnapshot(
    bool Enabled,
    bool Available,
    bool Playing,
    string Title,
    string Artist,
    string Source,
    float Rms,
    float Peak,
    float[] Bands,
    string Status
)
{
    internal static AudioSnapshot Empty(int bands) =>
        new(false, false, false, "", "", "", 0, 0, new float[bands], "");
}

internal readonly record struct SpectrumFrame(float Rms, float Peak, float[] Bands);

internal sealed class SpectrumAnalyzer
{
    private const int TransformSize = 2048;
    private const int TransformPower = 11;
    private readonly float[] input = new float[TransformSize];
    private readonly float[] smoothed;
    private int inputCount;

    internal SpectrumAnalyzer(int bands) => smoothed = new float[bands];

    internal void Reset()
    {
        inputCount = 0;
        Array.Clear(input);
        Array.Clear(smoothed);
    }

    internal SpectrumFrame? AddPcm(ReadOnlySpan<byte> data, WaveFormat format)
    {
        var bytesPerSample = Math.Max(1, format.BitsPerSample / 8);
        var bytesPerFrame = format.BlockAlign;
        var channels = Math.Max(1, format.Channels);
        SpectrumFrame? latest = null;
        for (var frameOffset = 0; frameOffset + bytesPerFrame <= data.Length; frameOffset += bytesPerFrame)
        {
            var mono = 0f;
            for (var channel = 0; channel < channels; channel++)
            {
                var offset = frameOffset + channel * bytesPerSample;
                mono += ReadSample(data.Slice(offset, bytesPerSample), format);
            }
            latest = AddSample(mono / channels, format.SampleRate) ?? latest;
        }
        return latest;
    }

    internal SpectrumFrame? AddPcm16(ReadOnlySpan<byte> data, int channels)
    {
        const int bytesPerSample = 2;
        var bytesPerFrame = bytesPerSample * channels;
        SpectrumFrame? latest = null;
        for (var offset = 0; offset + bytesPerFrame <= data.Length; offset += bytesPerFrame)
        {
            var mono = 0f;
            for (var channel = 0; channel < channels; channel++)
            {
                var sampleOffset = offset + channel * bytesPerSample;
                mono += (short)(data[sampleOffset] | (data[sampleOffset + 1] << 8)) / 32768f;
            }
            latest = AddSample(mono / channels, ProcessLoopbackCapture.SampleRate) ?? latest;
        }
        return latest;
    }

    internal SpectrumFrame? AddSilence(int frames)
    {
        SpectrumFrame? latest = null;
        for (var i = 0; i < frames; i++)
            latest = AddSample(0, ProcessLoopbackCapture.SampleRate) ?? latest;
        return latest;
    }

    private static float ReadSample(ReadOnlySpan<byte> bytes, WaveFormat format)
    {
        var isFloat = format.Encoding == WaveFormatEncoding.IeeeFloat;
        if (format is WaveFormatExtensible extensible)
            isFloat = extensible.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");

        if (isFloat && bytes.Length >= 4)
            return BitConverter.Int32BitsToSingle(
                bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24)
            );
        if (bytes.Length == 2)
            return (short)(bytes[0] | (bytes[1] << 8)) / 32768f;
        if (bytes.Length == 3)
        {
            var value = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
            if ((value & 0x800000) != 0)
                value |= unchecked((int)0xff000000);
            return value / 8388608f;
        }
        if (bytes.Length >= 4)
            return (bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24))
                / 2147483648f;
        return (bytes[0] - 128) / 128f;
    }

    private SpectrumFrame? AddSample(float sample, int sampleRate)
    {
        input[inputCount++] = float.IsFinite(sample) ? Math.Clamp(sample, -1, 1) : 0;
        if (inputCount < TransformSize)
            return null;
        inputCount = 0;
        return Analyze(sampleRate);
    }

    private SpectrumFrame Analyze(int sampleRate)
    {
        var fft = new NAudio.Dsp.Complex[TransformSize];
        double squareSum = 0;
        float peak = 0;
        for (var i = 0; i < TransformSize; i++)
        {
            var sample = input[i];
            squareSum += sample * sample;
            peak = Math.Max(peak, Math.Abs(sample));
            fft[i].X = (float)(
                sample * FastFourierTransform.HammingWindow(i, TransformSize)
            );
        }
        FastFourierTransform.FFT(true, TransformPower, fft);

        const float minimumFrequency = 45;
        var maximumFrequency = Math.Min(18000, sampleRate * 0.48f);
        var output = new float[smoothed.Length];
        for (var band = 0; band < output.Length; band++)
        {
            var low = minimumFrequency
                * MathF.Pow(maximumFrequency / minimumFrequency, band / (float)output.Length);
            var high = minimumFrequency
                * MathF.Pow(
                    maximumFrequency / minimumFrequency,
                    (band + 1f) / output.Length
                );
            var firstBin = Math.Clamp(
                (int)MathF.Floor(low * TransformSize / sampleRate),
                1,
                TransformSize / 2
            );
            var lastBin = Math.Clamp(
                (int)MathF.Ceiling(high * TransformSize / sampleRate),
                firstBin + 1,
                TransformSize / 2
            );
            var magnitude = 0f;
            for (var bin = firstBin; bin < lastBin; bin++)
                magnitude = Math.Max(magnitude, fft[bin].X * fft[bin].X + fft[bin].Y * fft[bin].Y);
            magnitude = MathF.Sqrt(magnitude) * (2f / 0.54f);
            var decibels = 20 * MathF.Log10(Math.Max(magnitude, 0.000001f));
            var target = MathF.Pow(Math.Clamp((decibels + 72) / 66, 0, 1), 0.72f);
            var response = target > smoothed[band] ? 0.68f : 0.16f;
            smoothed[band] += (target - smoothed[band]) * response;
            output[band] = smoothed[band];
        }
        return new(
            (float)Math.Sqrt(squareSum / TransformSize),
            peak,
            output
        );
    }
}

internal sealed class ProcessLoopbackCapture : IDisposable
{
    internal const int SampleRate = 44100;
    internal const int Channels = 2;
    internal const int BlockAlign = Channels * sizeof(short);
    private const string ProcessLoopbackDevice = "VAD\\Process_Loopback";
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task worker;

    internal ProcessLoopbackCapture(
        int processId,
        Action<nint, uint, bool> samples,
        Action ready,
        Action<string> failed
    )
    {
        worker = Task.Run(() => Capture(processId, samples, ready, failed, cancellation.Token));
    }

    private static void Capture(
        int processId,
        Action<nint, uint, bool> samples,
        Action ready,
        Action<string> failed,
        CancellationToken cancellationToken
    )
    {
        IAudioClient? audioClient = null;
        IAudioCaptureClient? captureClient = null;
        try
        {
            audioClient = Activate(processId);
            var format = new WaveFormatEx
            {
                FormatTag = 1,
                Channels = Channels,
                SamplesPerSecond = SampleRate,
                AverageBytesPerSecond = SampleRate * BlockAlign,
                BlockAlign = BlockAlign,
                BitsPerSample = 16,
            };
            var formatPointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());
            try
            {
                Marshal.StructureToPtr(format, formatPointer, false);
                ThrowIfFailed(
                    audioClient.Initialize(
                        AudioClientShareMode.Shared,
                        AudioClientStreamFlags.Loopback
                            | AudioClientStreamFlags.AutoConvertPcm
                            | AudioClientStreamFlags.SourceDefaultQuality,
                        0,
                        0,
                        formatPointer,
                        nint.Zero
                    )
                );
            }
            finally
            {
                Marshal.FreeHGlobal(formatPointer);
            }

            var captureClientId = typeof(IAudioCaptureClient).GUID;
            ThrowIfFailed(audioClient.GetService(ref captureClientId, out var service));
            captureClient = (IAudioCaptureClient)service;
            ThrowIfFailed(audioClient.Start());
            ready();

            while (!cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(8);
                while (true)
                {
                    ThrowIfFailed(captureClient.GetNextPacketSize(out var frames));
                    if (frames == 0)
                        break;
                    ThrowIfFailed(
                        captureClient.GetBuffer(
                            out var data,
                            out frames,
                            out var flags,
                            out _,
                            out _
                        )
                    );
                    try
                    {
                        samples(data, frames, (flags & AudioClientBufferFlags.Silent) != 0);
                    }
                    finally
                    {
                        captureClient.ReleaseBuffer(frames);
                    }
                }
            }
            audioClient.Stop();
        }
        catch (Exception exception)
        {
            failed(exception.Message);
        }
        finally
        {
            if (captureClient is not null && Marshal.IsComObject(captureClient))
                Marshal.FinalReleaseComObject(captureClient);
            if (audioClient is not null && Marshal.IsComObject(audioClient))
                Marshal.FinalReleaseComObject(audioClient);
        }
    }

    private static IAudioClient Activate(int processId)
    {
        var activation = new AudioClientActivationParams
        {
            ActivationType = AudioClientActivationType.ProcessLoopback,
            ProcessLoopbackParams = new AudioClientProcessLoopbackParams
            {
                TargetProcessId = (uint)processId,
                ProcessLoopbackMode = ProcessLoopbackMode.IncludeTargetProcessTree,
            },
        };
        var activationPointer = Marshal.AllocHGlobal(Marshal.SizeOf<AudioClientActivationParams>());
        var variantPointer = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
        try
        {
            Marshal.StructureToPtr(activation, activationPointer, false);
            var variant = new PropVariant
            {
                VariantType = 65,
                Blob = new Blob
                {
                    Size = Marshal.SizeOf<AudioClientActivationParams>(),
                    Data = activationPointer,
                },
            };
            Marshal.StructureToPtr(variant, variantPointer, false);

            using var completed = new ManualResetEventSlim();
            var handler = new ActivationHandler(completed);
            var audioClientId = typeof(IAudioClient).GUID;
            ThrowIfFailed(
                ActivateAudioInterfaceAsync(
                    ProcessLoopbackDevice,
                    ref audioClientId,
                    variantPointer,
                    handler,
                    out var operation
                )
            );
            if (!completed.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("Windows did not complete application audio activation");
            GC.KeepAlive(operation);
            return handler.GetResult();
        }
        finally
        {
            Marshal.FreeHGlobal(variantPointer);
            Marshal.FreeHGlobal(activationPointer);
        }
    }

    private static void ThrowIfFailed(int result)
    {
        if (result < 0)
            Marshal.ThrowExceptionForHR(result);
    }

    public void Dispose()
    {
        cancellation.Cancel();
        try
        {
            worker.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException exception)
            when (exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        { }
        cancellation.Dispose();
    }

    [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int ActivateAudioInterfaceAsync(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
        ref Guid interfaceId,
        nint activationParams,
        IActivateAudioInterfaceCompletionHandler completionHandler,
        out IActivateAudioInterfaceAsyncOperation operation
    );

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class ActivationHandler : IActivateAudioInterfaceCompletionHandler
    {
        private readonly ManualResetEventSlim completed;
        private int activationResult;
        private object? activatedInterface;

        internal ActivationHandler(ManualResetEventSlim completed) => this.completed = completed;

        public int ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
        {
            try
            {
                var result = operation.GetActivateResult(
                    out activationResult,
                    out activatedInterface
                );
                if (result < 0)
                    activationResult = result;
            }
            catch (Exception exception)
            {
                activationResult = exception.HResult;
            }
            finally
            {
                completed.Set();
            }
            return 0;
        }

        internal IAudioClient GetResult()
        {
            ThrowIfFailed(activationResult);
            return (IAudioClient)(activatedInterface
                ?? throw new InvalidOperationException("Windows returned no audio client"));
        }
    }

    [Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceCompletionHandler
    {
        [PreserveSig]
        int ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation);
    }

    [ComImport]
    [Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IActivateAudioInterfaceAsyncOperation
    {
        [PreserveSig]
        int GetActivateResult(
            out int activateResult,
            [MarshalAs(UnmanagedType.IUnknown)] out object? activatedInterface
        );
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig]
        int Initialize(
            AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            nint format,
            nint audioSessionGuid
        );

        [PreserveSig]
        int GetBufferSize(out uint bufferFrames);

        [PreserveSig]
        int GetStreamLatency(out long latency);

        [PreserveSig]
        int GetCurrentPadding(out uint paddingFrames);

        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            nint format,
            out nint closestMatch
        );

        [PreserveSig]
        int GetMixFormat(out nint format);

        [PreserveSig]
        int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);

        [PreserveSig]
        int Start();

        [PreserveSig]
        int Stop();

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int SetEventHandle(nint eventHandle);

        [PreserveSig]
        int GetService(ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport]
    [Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig]
        int GetBuffer(
            out nint data,
            out uint frames,
            out AudioClientBufferFlags flags,
            out ulong devicePosition,
            out ulong performanceCounterPosition
        );

        [PreserveSig]
        int ReleaseBuffer(uint frames);

        [PreserveSig]
        int GetNextPacketSize(out uint frames);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WaveFormatEx
    {
        internal ushort FormatTag;
        internal ushort Channels;
        internal uint SamplesPerSecond;
        internal uint AverageBytesPerSecond;
        internal ushort BlockAlign;
        internal ushort BitsPerSample;
        internal ushort ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientActivationParams
    {
        internal AudioClientActivationType ActivationType;
        internal AudioClientProcessLoopbackParams ProcessLoopbackParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioClientProcessLoopbackParams
    {
        internal uint TargetProcessId;
        internal ProcessLoopbackMode ProcessLoopbackMode;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)]
        internal ushort VariantType;

        [FieldOffset(8)]
        internal Blob Blob;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Blob
    {
        internal int Size;
        internal nint Data;
    }

    private enum AudioClientActivationType
    {
        Default,
        ProcessLoopback,
    }

    private enum ProcessLoopbackMode
    {
        IncludeTargetProcessTree,
        ExcludeTargetProcessTree,
    }

    private enum AudioClientShareMode
    {
        Shared,
        Exclusive,
    }

    [Flags]
    private enum AudioClientStreamFlags : uint
    {
        Loopback = 0x00020000,
        SourceDefaultQuality = 0x08000000,
        AutoConvertPcm = 0x80000000,
    }

    [Flags]
    private enum AudioClientBufferFlags : uint
    {
        Silent = 0x2,
    }
}
