using System.Buffers.Binary;
using ForzaOSD.Core;

namespace ForzaOSD.Tests;

public sealed class DecoderTests
{
    private static byte[] Packet(int size = 324)
    {
        var p = new byte[size];
        var horizon = size is 323 or 324;
        var tail = horizon ? 244 : 232;
        I32(p, 0, 1);
        U32(p, 4, 123456);
        F32(p, 8, 8500);
        F32(p, 12, 900);
        F32(p, 16, 6750);
        F32(p, 20, 9.80665f);
        F32(p, 24, 2.5f);
        F32(p, 28, -4.903325f);
        F32(p, 32, 10);
        F32(p, 36, -2);
        F32(p, 40, 30);
        F32(p, 44, .1f);
        F32(p, 48, .2f);
        F32(p, 52, -.3f);
        F32(p, 56, 1.1f);
        F32(p, 60, -.4f);
        F32(p, 64, .25f);
        F32(p, 68, .1f);
        F32(p, 72, .2f);
        F32(p, 76, .3f);
        F32(p, 80, .4f);
        F32(p, 84, -.5f);
        F32(p, 88, .6f);
        F32(p, 92, -1.1f);
        F32(p, 96, 1.2f);
        F32(p, 100, -30);
        F32(p, 104, 31);
        F32(p, 108, -32);
        F32(p, 112, 33);
        I32(p, 116, 1);
        I32(p, 120, 0);
        I32(p, 124, -1);
        I32(p, 128, 0);
        I32(p, 132, 0);
        I32(p, 136, 1);
        I32(p, 140, 0);
        I32(p, 144, -1);
        F32(p, 148, .11f);
        F32(p, 152, .22f);
        F32(p, 156, .33f);
        F32(p, 160, .44f);
        F32(p, 164, -.51f);
        F32(p, 168, .62f);
        F32(p, 172, -.73f);
        F32(p, 176, .84f);
        F32(p, 180, .91f);
        F32(p, 184, 1.01f);
        F32(p, 188, 1.11f);
        F32(p, 192, 1.21f);
        F32(p, 196, .051f);
        F32(p, 200, .062f);
        F32(p, 204, .073f);
        F32(p, 208, .084f);
        I32(p, 212, 1234);
        I32(p, 216, 4);
        I32(p, 220, 812);
        I32(p, 224, 2);
        I32(p, 228, 8);
        if (horizon)
        {
            U32(p, 232, 77);
            F32(p, 236, 12.5f);
            F32(p, 240, 350);
        }
        F32(p, tail, -7104.75f);
        F32(p, tail + 4, 88.5f);
        F32(p, tail + 8, -1863.25f);
        F32(p, tail + 12, 50);
        F32(p, tail + 16, -12345);
        F32(p, tail + 20, 456.75f);
        F32(p, tail + 24, 181);
        F32(p, tail + 28, 184);
        F32(p, tail + 32, 190);
        F32(p, tail + 36, 193);
        F32(p, tail + 40, 1.25f);
        F32(p, tail + 44, .75f);
        F32(p, tail + 48, 54321);
        F32(p, tail + 52, 62.5f);
        F32(p, tail + 56, 64.25f);
        F32(p, tail + 60, 18.75f);
        F32(p, tail + 64, 245.5f);
        U16(p, tail + 68, 3);
        p[tail + 70] = 7;
        p[tail + 71] = 128;
        p[tail + 72] = 64;
        p[tail + 73] = 32;
        p[tail + 74] = 1;
        p[tail + 75] = 5;
        p[tail + 76] = unchecked((byte)-64);
        p[tail + 77] = 63;
        p[tail + 78] = unchecked((byte)-127);
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

    [Theory]
    [InlineData(323)]
    [InlineData(324)]
    public void CompleteHorizonPacketDecodes(int size)
    {
        var frame = ForzaDashDecoder.Decode(Packet(size)).Frame;

        Assert.Equal(123456u, frame.GameTimestampMs);
        Assert.Equal(2.5f, frame.AccelerationY);
        Assert.Equal((10f, -2f, 30f), (frame.VelocityX, frame.VelocityY, frame.VelocityZ));
        Assert.Equal((.1f, .2f, -.3f), (frame.AngularVelocityX, frame.AngularVelocityY, frame.AngularVelocityZ));
        Assert.Equal((1.1f, -.4f, .25f), (frame.Yaw, frame.Pitch, frame.Roll));
        Assert.Equal(
            (.1f, .2f, .3f, .4f),
            (
                frame.NormalizedSuspensionTravelFrontLeft,
                frame.NormalizedSuspensionTravelFrontRight,
                frame.NormalizedSuspensionTravelRearLeft,
                frame.NormalizedSuspensionTravelRearRight
            )
        );
        Assert.Equal(
            (-.5f, .6f, -1.1f, 1.2f),
            (
                frame.TireSlipRatioFrontLeft,
                frame.TireSlipRatioFrontRight,
                frame.TireSlipRatioRearLeft,
                frame.TireSlipRatioRearRight
            )
        );
        Assert.Equal(
            (-30f, 31f, -32f, 33f),
            (
                frame.WheelRotationSpeedFrontLeft,
                frame.WheelRotationSpeedFrontRight,
                frame.WheelRotationSpeedRearLeft,
                frame.WheelRotationSpeedRearRight
            )
        );
        Assert.Equal(
            (true, false, true, false),
            (
                frame.WheelOnRumbleStripFrontLeft,
                frame.WheelOnRumbleStripFrontRight,
                frame.WheelOnRumbleStripRearLeft,
                frame.WheelOnRumbleStripRearRight
            )
        );
        Assert.Equal(
            (false, true, false, true),
            (
                frame.WheelInPuddleFrontLeft,
                frame.WheelInPuddleFrontRight,
                frame.WheelInPuddleRearLeft,
                frame.WheelInPuddleRearRight
            )
        );
        Assert.Equal(
            (.11f, .22f, .33f, .44f),
            (
                frame.SurfaceRumbleFrontLeft,
                frame.SurfaceRumbleFrontRight,
                frame.SurfaceRumbleRearLeft,
                frame.SurfaceRumbleRearRight
            )
        );
        Assert.Equal(
            (-.51f, .62f, -.73f, .84f),
            (
                frame.TireSlipAngleFrontLeft,
                frame.TireSlipAngleFrontRight,
                frame.TireSlipAngleRearLeft,
                frame.TireSlipAngleRearRight
            )
        );
        Assert.Equal(
            (.91f, 1.01f, 1.11f, 1.21f),
            (
                frame.TireCombinedSlipFrontLeft,
                frame.TireCombinedSlipFrontRight,
                frame.TireCombinedSlipRearLeft,
                frame.TireCombinedSlipRearRight
            )
        );
        Assert.Equal(
            (.051f, .062f, .073f, .084f),
            (
                frame.SuspensionTravelMetersFrontLeft,
                frame.SuspensionTravelMetersFrontRight,
                frame.SuspensionTravelMetersRearLeft,
                frame.SuspensionTravelMetersRearRight
            )
        );
        Assert.Equal((4, 812, 2, 8), (frame.CarClass, frame.CarPerformanceIndex, frame.DrivetrainType, frame.NumCylinders));
        Assert.Equal(77u, frame.CarGroup);
        Assert.Equal(12.5f, frame.SmashableVelocityDifference);
        Assert.Equal(350f, frame.SmashableMass);
        Assert.Equal((-7104.75f, 88.5f, -1863.25f), (frame.PositionX, frame.PositionY, frame.PositionZ));
        Assert.Equal(-12345f, frame.PowerWatts);
        Assert.Equal(456.75f, frame.TorqueNm);
        Assert.Equal(54321f, frame.DistanceTraveledMeters);
        Assert.Equal((62.5f, 64.25f, 18.75f, 245.5f), (frame.BestLapSeconds, frame.LastLapSeconds, frame.CurrentLapSeconds, frame.RaceTimeSeconds));
        Assert.Equal(32 / 255f, frame.Clutch);
        Assert.Equal(-64, frame.SteeringRaw);
        Assert.Equal(-64 / 127f, frame.Steering);
        Assert.Equal(63, frame.DrivingLineRaw);
        Assert.Equal(63 / 127f, frame.DrivingLine);
        Assert.Equal(-127, frame.AiBrakeDifferenceRaw);
        Assert.Equal(-1, frame.AiBrakeDifference);
    }

    [Fact]
    public void MotorsportPacketDefaultsHorizonOnlyFields()
    {
        var frame = ForzaDashDecoder.Decode(Packet(311)).Frame;
        Assert.Equal(0u, frame.CarGroup);
        Assert.Equal(0, frame.SmashableVelocityDifference);
        Assert.Equal(0, frame.SmashableMass);
        Assert.Equal(-7104.75f, frame.PositionX);
        Assert.Equal(-127, frame.AiBrakeDifferenceRaw);
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
    public void InvalidOptionalMotionAndPowerAreSanitized()
    {
        var packet = Packet();
        F32(packet, 32, float.PositiveInfinity);
        F32(packet, 260, float.NaN);
        var frame = ForzaDashDecoder.Decode(packet).Frame;
        Assert.Equal(0, frame.VelocityX);
        Assert.Equal(0, frame.PowerWatts);
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
