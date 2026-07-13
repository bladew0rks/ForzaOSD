using System.Buffers.Binary;
using ForzaOSD.Core;

namespace ForzaOSD.Tests;

public sealed class DecoderTests
{
    private static byte[] Packet(int size = 324)
    {
        var p = new byte[size];
        var shift = size is 323 or 324 ? 12 : 0;
        I32(p, 0, 1);
        U32(p, 4, 123456);
        F32(p, 8, 8500);
        F32(p, 12, 900);
        F32(p, 16, 6750);
        F32(p, 20, 9.80665f);
        F32(p, 28, -4.903325f);
        I32(p, 212, 1234);
        F32(p, 244 + shift, 50);
        F32(p, 256 + shift, 181);
        F32(p, 260 + shift, 184);
        F32(p, 264 + shift, 190);
        F32(p, 268 + shift, 193);
        F32(p, 272 + shift, 1.25f);
        F32(p, 276 + shift, .75f);
        U16(p, 300 + shift, 3);
        p[302 + shift] = 7;
        p[303 + shift] = 128;
        p[304 + shift] = 64;
        p[306 + shift] = 1;
        p[307 + shift] = 5;
        p[308 + shift] = unchecked((byte)-64);
        return p;
    }

    private static void I32(byte[] p, int o, int v) =>
        BinaryPrimitives.WriteInt32LittleEndian(p.AsSpan(o), v);

    private static void U32(byte[] p, int o, uint v) =>
        BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(o), v);

    private static void U16(byte[] p, int o, ushort v) =>
        BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(o), v);

    private static void F32(byte[] p, int o, float v) =>
        I32(p, o, BitConverter.SingleToInt32Bits(v));

    [Theory]
    [InlineData(311)]
    [InlineData(323)]
    [InlineData(324)]
    public void SupportedPacketDecodes(int size)
    {
        var result = ForzaDashDecoder.Decode(Packet(size));
        Assert.Equal(DecodeStatus.Decoded, result.Status);
        Assert.Equal(8500, result.Frame.EngineMaxRpm);
        Assert.Equal(900, result.Frame.EngineIdleRpm);
        Assert.Equal(6750, result.Frame.EngineRpm);
        Assert.Equal(1234, result.Frame.CarOrdinal);
        Assert.Equal(50, result.Frame.SpeedMps);
        Assert.Equal(5, result.Frame.Gear);
        Assert.Equal(1, result.Frame.LateralG, 3);
        Assert.Equal(-.5, result.Frame.LongitudinalG, 3);
        Assert.Equal(181, result.Frame.TireTempFrontLeft);
        Assert.Equal(184, result.Frame.TireTempFrontRight);
        Assert.Equal(190, result.Frame.TireTempRearLeft);
        Assert.Equal(193, result.Frame.TireTempRearRight);
        Assert.Equal(3, result.Frame.LapNumber);
        Assert.Equal(7, result.Frame.RacePosition);
    }

    [Fact]
    public void VacuumIsOptional()
    {
        var packet = Packet();
        F32(packet, 284, -14.7f);
        var result = ForzaDashDecoder.Decode(packet);
        Assert.Equal(DecodeStatus.Decoded, result.Status);
        Assert.Equal(-14.7f, result.Frame.Boost);
    }

    [Fact]
    public void InvalidOptionalTireTemperatureIsSanitized()
    {
        var packet = Packet();
        F32(packet, 268, float.NaN);
        var result = ForzaDashDecoder.Decode(packet);
        Assert.Equal(DecodeStatus.Decoded, result.Status);
        Assert.Equal(0, result.Frame.TireTempFrontLeft);
        Assert.Equal(184, result.Frame.TireTempFrontRight);
    }

    [Fact]
    public void InvalidOptionalIdleRpmIsSanitized()
    {
        var packet = Packet();
        F32(packet, 12, float.NaN);
        var result = ForzaDashDecoder.Decode(packet);
        Assert.Equal(DecodeStatus.Decoded, result.Status);
        Assert.Equal(0, result.Frame.EngineIdleRpm);
        Assert.Equal(6750, result.Frame.EngineRpm);
    }

    [Fact]
    public void UnknownSizeRejected() =>
        Assert.Equal(DecodeStatus.UnsupportedSize, ForzaDashDecoder.Decode(new byte[100]).Status);

    [Theory]
    [InlineData(0, "R")]
    [InlineData(1, "1")]
    [InlineData(11, "N")]
    public void GearLabels(byte raw, string label) =>
        Assert.Equal(label, GearFormatter.Format(raw));
}
