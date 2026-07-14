using System.Buffers.Binary;
using System.Diagnostics;

namespace ForzaOSD.Core;

public sealed record TelemetryFrame
{
    public long ReceivedTimestamp { get; init; }
    public uint GameTimestampMs { get; init; }
    public bool RaceOn { get; init; }
    public float EngineMaxRpm { get; init; }
    public float EngineIdleRpm { get; init; }
    public float EngineRpm { get; init; }
    public float AccelerationX { get; init; }
    public float AccelerationY { get; init; }
    public float AccelerationZ { get; init; }
    public float VelocityX { get; init; }
    public float VelocityY { get; init; }
    public float VelocityZ { get; init; }
    public float AngularVelocityX { get; init; }
    public float AngularVelocityY { get; init; }
    public float AngularVelocityZ { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public float Roll { get; init; }
    public float NormalizedSuspensionTravelFrontLeft { get; init; }
    public float NormalizedSuspensionTravelFrontRight { get; init; }
    public float NormalizedSuspensionTravelRearLeft { get; init; }
    public float NormalizedSuspensionTravelRearRight { get; init; }
    public float TireSlipRatioFrontLeft { get; init; }
    public float TireSlipRatioFrontRight { get; init; }
    public float TireSlipRatioRearLeft { get; init; }
    public float TireSlipRatioRearRight { get; init; }
    public float WheelRotationSpeedFrontLeft { get; init; }
    public float WheelRotationSpeedFrontRight { get; init; }
    public float WheelRotationSpeedRearLeft { get; init; }
    public float WheelRotationSpeedRearRight { get; init; }
    public bool WheelOnRumbleStripFrontLeft { get; init; }
    public bool WheelOnRumbleStripFrontRight { get; init; }
    public bool WheelOnRumbleStripRearLeft { get; init; }
    public bool WheelOnRumbleStripRearRight { get; init; }
    public bool WheelInPuddleFrontLeft { get; init; }
    public bool WheelInPuddleFrontRight { get; init; }
    public bool WheelInPuddleRearLeft { get; init; }
    public bool WheelInPuddleRearRight { get; init; }
    public float SurfaceRumbleFrontLeft { get; init; }
    public float SurfaceRumbleFrontRight { get; init; }
    public float SurfaceRumbleRearLeft { get; init; }
    public float SurfaceRumbleRearRight { get; init; }
    public float TireSlipAngleFrontLeft { get; init; }
    public float TireSlipAngleFrontRight { get; init; }
    public float TireSlipAngleRearLeft { get; init; }
    public float TireSlipAngleRearRight { get; init; }
    public float TireCombinedSlipFrontLeft { get; init; }
    public float TireCombinedSlipFrontRight { get; init; }
    public float TireCombinedSlipRearLeft { get; init; }
    public float TireCombinedSlipRearRight { get; init; }
    public float SuspensionTravelMetersFrontLeft { get; init; }
    public float SuspensionTravelMetersFrontRight { get; init; }
    public float SuspensionTravelMetersRearLeft { get; init; }
    public float SuspensionTravelMetersRearRight { get; init; }
    public int CarOrdinal { get; init; }
    public int CarClass { get; init; }
    public int CarPerformanceIndex { get; init; }
    public int DrivetrainType { get; init; }
    public int NumCylinders { get; init; }
    public uint CarGroup { get; init; }
    public float SmashableVelocityDifference { get; init; }
    public float SmashableMass { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float PositionZ { get; init; }
    public float SpeedMps { get; init; }
    public float PowerWatts { get; init; }
    public float TorqueNm { get; init; }
    public float TireTempFrontLeft { get; init; }
    public float TireTempFrontRight { get; init; }
    public float TireTempRearLeft { get; init; }
    public float TireTempRearRight { get; init; }
    public float Boost { get; init; }
    public float Fuel { get; init; }
    public float DistanceTraveledMeters { get; init; }
    public float BestLapSeconds { get; init; }
    public float LastLapSeconds { get; init; }
    public float CurrentLapSeconds { get; init; }
    public float RaceTimeSeconds { get; init; }
    public ushort LapNumber { get; init; }
    public byte RacePosition { get; init; }
    public byte Gear { get; init; }
    public float Throttle { get; init; }
    public float Brake { get; init; }
    public float Clutch { get; init; }
    public bool Handbrake { get; init; }
    public sbyte SteeringRaw { get; init; }
    public float Steering { get; init; }
    public sbyte DrivingLineRaw { get; init; }
    public float DrivingLine { get; init; }
    public sbyte AiBrakeDifferenceRaw { get; init; }
    public float AiBrakeDifference { get; init; }
    public float LateralG { get; init; }
    public float LongitudinalG { get; init; }
}

public enum DecodeStatus
{
    Decoded,
    UnsupportedSize,
    Malformed,
}

public sealed record DecodeResult(
    DecodeStatus Status,
    TelemetryFrame Frame,
    string Format,
    string Detail
);

public static class ForzaDashDecoder
{
    private static float F32(ReadOnlySpan<byte> p, int offset) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(p[offset..]));

    private static bool Sane(float value, float low, float high) =>
        float.IsFinite(value) && value >= low && value <= high;

    private static float Finite(float value) => float.IsFinite(value) ? value : 0;

    private static float NormalizedInput(sbyte value) => Math.Clamp(value / 127f, -1, 1);

    public static DecodeResult Decode(ReadOnlySpan<byte> packet)
    {
        var horizon = packet.Length is 323 or 324;
        var motorsport = packet.Length == 311;
        if (!horizon && !motorsport)
            return new(
                DecodeStatus.UnsupportedSize,
                new(),
                "Unsupported packet",
                $"Unknown datagram size: {packet.Length} bytes"
            );

        var tail = horizon ? 244 : 232;
        var race = BinaryPrimitives.ReadInt32LittleEndian(packet);
        var maxRpm = F32(packet, 8);
        var idleRpm = F32(packet, 12);
        var rpm = F32(packet, 16);
        var accelerationX = F32(packet, 20);
        var accelerationZ = F32(packet, 28);
        var speed = F32(packet, tail + 12);
        if (
            race is not (0 or 1)
            || !Sane(maxRpm, 0, 30_000)
            || !Sane(rpm, -100, 30_000)
            || !Sane(speed, -10, 500)
        )
            return new(
                DecodeStatus.Malformed,
                new(),
                "Malformed packet",
                "Packet fields failed validation"
            );

        var steer = unchecked((sbyte)packet[tail + 76]);
        var drivingLine = unchecked((sbyte)packet[tail + 77]);
        var aiBrakeDifference = unchecked((sbyte)packet[tail + 78]);
        var frame = new TelemetryFrame
        {
            ReceivedTimestamp = Stopwatch.GetTimestamp(),
            GameTimestampMs = BinaryPrimitives.ReadUInt32LittleEndian(packet[4..]),
            RaceOn = race == 1,
            EngineMaxRpm = maxRpm,
            EngineIdleRpm = Sane(idleRpm, 0, 30_000) ? idleRpm : 0,
            EngineRpm = rpm,
            AccelerationX = Finite(accelerationX),
            AccelerationY = Finite(F32(packet, 24)),
            AccelerationZ = Finite(accelerationZ),
            VelocityX = Finite(F32(packet, 32)),
            VelocityY = Finite(F32(packet, 36)),
            VelocityZ = Finite(F32(packet, 40)),
            AngularVelocityX = Finite(F32(packet, 44)),
            AngularVelocityY = Finite(F32(packet, 48)),
            AngularVelocityZ = Finite(F32(packet, 52)),
            Yaw = Finite(F32(packet, 56)),
            Pitch = Finite(F32(packet, 60)),
            Roll = Finite(F32(packet, 64)),
            NormalizedSuspensionTravelFrontLeft = Finite(F32(packet, 68)),
            NormalizedSuspensionTravelFrontRight = Finite(F32(packet, 72)),
            NormalizedSuspensionTravelRearLeft = Finite(F32(packet, 76)),
            NormalizedSuspensionTravelRearRight = Finite(F32(packet, 80)),
            TireSlipRatioFrontLeft = Finite(F32(packet, 84)),
            TireSlipRatioFrontRight = Finite(F32(packet, 88)),
            TireSlipRatioRearLeft = Finite(F32(packet, 92)),
            TireSlipRatioRearRight = Finite(F32(packet, 96)),
            WheelRotationSpeedFrontLeft = Finite(F32(packet, 100)),
            WheelRotationSpeedFrontRight = Finite(F32(packet, 104)),
            WheelRotationSpeedRearLeft = Finite(F32(packet, 108)),
            WheelRotationSpeedRearRight = Finite(F32(packet, 112)),
            WheelOnRumbleStripFrontLeft = BinaryPrimitives.ReadInt32LittleEndian(packet[116..]) != 0,
            WheelOnRumbleStripFrontRight = BinaryPrimitives.ReadInt32LittleEndian(packet[120..]) != 0,
            WheelOnRumbleStripRearLeft = BinaryPrimitives.ReadInt32LittleEndian(packet[124..]) != 0,
            WheelOnRumbleStripRearRight = BinaryPrimitives.ReadInt32LittleEndian(packet[128..]) != 0,
            WheelInPuddleFrontLeft = BinaryPrimitives.ReadInt32LittleEndian(packet[132..]) != 0,
            WheelInPuddleFrontRight = BinaryPrimitives.ReadInt32LittleEndian(packet[136..]) != 0,
            WheelInPuddleRearLeft = BinaryPrimitives.ReadInt32LittleEndian(packet[140..]) != 0,
            WheelInPuddleRearRight = BinaryPrimitives.ReadInt32LittleEndian(packet[144..]) != 0,
            SurfaceRumbleFrontLeft = Finite(F32(packet, 148)),
            SurfaceRumbleFrontRight = Finite(F32(packet, 152)),
            SurfaceRumbleRearLeft = Finite(F32(packet, 156)),
            SurfaceRumbleRearRight = Finite(F32(packet, 160)),
            TireSlipAngleFrontLeft = Finite(F32(packet, 164)),
            TireSlipAngleFrontRight = Finite(F32(packet, 168)),
            TireSlipAngleRearLeft = Finite(F32(packet, 172)),
            TireSlipAngleRearRight = Finite(F32(packet, 176)),
            TireCombinedSlipFrontLeft = Finite(F32(packet, 180)),
            TireCombinedSlipFrontRight = Finite(F32(packet, 184)),
            TireCombinedSlipRearLeft = Finite(F32(packet, 188)),
            TireCombinedSlipRearRight = Finite(F32(packet, 192)),
            SuspensionTravelMetersFrontLeft = Finite(F32(packet, 196)),
            SuspensionTravelMetersFrontRight = Finite(F32(packet, 200)),
            SuspensionTravelMetersRearLeft = Finite(F32(packet, 204)),
            SuspensionTravelMetersRearRight = Finite(F32(packet, 208)),
            CarOrdinal = BinaryPrimitives.ReadInt32LittleEndian(packet[212..]),
            CarClass = BinaryPrimitives.ReadInt32LittleEndian(packet[216..]),
            CarPerformanceIndex = BinaryPrimitives.ReadInt32LittleEndian(packet[220..]),
            DrivetrainType = BinaryPrimitives.ReadInt32LittleEndian(packet[224..]),
            NumCylinders = BinaryPrimitives.ReadInt32LittleEndian(packet[228..]),
            CarGroup = horizon ? BinaryPrimitives.ReadUInt32LittleEndian(packet[232..]) : 0,
            SmashableVelocityDifference = horizon ? Finite(F32(packet, 236)) : 0,
            SmashableMass = horizon ? Finite(F32(packet, 240)) : 0,
            PositionX = Finite(F32(packet, tail)),
            PositionY = Finite(F32(packet, tail + 4)),
            PositionZ = Finite(F32(packet, tail + 8)),
            SpeedMps = speed,
            PowerWatts = Finite(F32(packet, tail + 16)),
            TorqueNm = Finite(F32(packet, tail + 20)),
            TireTempFrontLeft = Finite(F32(packet, tail + 24)),
            TireTempFrontRight = Finite(F32(packet, tail + 28)),
            TireTempRearLeft = Finite(F32(packet, tail + 32)),
            TireTempRearRight = Finite(F32(packet, tail + 36)),
            Boost = Finite(F32(packet, tail + 40)),
            Fuel = Finite(F32(packet, tail + 44)),
            DistanceTraveledMeters = Finite(F32(packet, tail + 48)),
            BestLapSeconds = Finite(F32(packet, tail + 52)),
            LastLapSeconds = Finite(F32(packet, tail + 56)),
            CurrentLapSeconds = Finite(F32(packet, tail + 60)),
            RaceTimeSeconds = Finite(F32(packet, tail + 64)),
            LapNumber = BinaryPrimitives.ReadUInt16LittleEndian(packet[(tail + 68)..]),
            RacePosition = packet[tail + 70],
            Throttle = packet[tail + 71] / 255f,
            Brake = packet[tail + 72] / 255f,
            Clutch = packet[tail + 73] / 255f,
            Handbrake = packet[tail + 74] != 0,
            Gear = packet[tail + 75],
            SteeringRaw = steer,
            Steering = NormalizedInput(steer),
            DrivingLineRaw = drivingLine,
            DrivingLine = NormalizedInput(drivingLine),
            AiBrakeDifferenceRaw = aiBrakeDifference,
            AiBrakeDifference = NormalizedInput(aiBrakeDifference),
            LateralG = Finite(accelerationX) / 9.80665f,
            LongitudinalG = Finite(accelerationZ) / 9.80665f,
        };
        return new(
            DecodeStatus.Decoded,
            frame,
            horizon ? "Forza Horizon Dash (323/324)" : "Forza Motorsport Dash (311)",
            ""
        );
    }
}

public sealed record TelemetrySnapshot
{
    public TelemetryFrame Frame { get; init; } = new();
    public bool HasFrame { get; init; }
    public bool Stale { get; init; } = true;
    public double PacketsPerSecond { get; init; }
    public ulong PacketsReceived { get; init; }
    public ulong MalformedPackets { get; init; }
    public int LastPacketSize { get; init; }
    public string Format { get; init; } = "Waiting for UDP";
    public string Detail { get; init; } = "";

    public bool IsDriving => HasFrame && !Stale && Frame.RaceOn;

    public bool ShouldShow(bool editMode) => editMode || IsDriving;
}

public static class GearFormatter
{
    public static string Format(byte gear) =>
        gear switch
        {
            0 => "R",
            11 => "N",
            _ => gear.ToString(),
        };
}
