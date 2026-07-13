using ForzaOSD.App;

namespace ForzaOSD.Tests;

public sealed class AudioSpectrumTests
{
    [Fact]
    public void PcmToneProducesVisibleSpectrumBand()
    {
        const int frameCount = 2048;
        const float frequency = 1000;
        const float amplitude = 0.5f;
        var pcm = new byte[frameCount * ProcessLoopbackCapture.BlockAlign];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var phase = MathF.Tau * frequency * frame / ProcessLoopbackCapture.SampleRate;
            var sample = (short)MathF.Round(MathF.Sin(phase) * amplitude * short.MaxValue);
            for (var channel = 0; channel < ProcessLoopbackCapture.Channels; channel++)
            {
                var offset =
                    frame * ProcessLoopbackCapture.BlockAlign + channel * sizeof(short);
                pcm[offset] = (byte)sample;
                pcm[offset + 1] = (byte)(sample >> 8);
            }
        }

        var analyzer = new SpectrumAnalyzer(28);
        var result = analyzer.AddPcm16(pcm, ProcessLoopbackCapture.Channels);

        Assert.NotNull(result);
        Assert.InRange(result.Value.Rms, 0.34f, 0.36f);
        Assert.InRange(result.Value.Peak, 0.49f, 0.51f);
        Assert.True(result.Value.Bands.Max() > 0.6f);
    }
}
