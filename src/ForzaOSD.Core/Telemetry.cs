using System.Buffers.Binary;
using System.Diagnostics;

namespace ForzaOSD.Core;

public sealed record TelemetryFrame
{
    public long ReceivedTimestamp { get; init; }
    public uint GameTimestampMs { get; init; }
    public bool RaceOn { get; init; }
    public float SpeedMps { get; init; }
    public float EngineRpm { get; init; }
    public float EngineMaxRpm { get; init; }
    public float EngineIdleRpm { get; init; }
    public int CarOrdinal { get; init; }
    public byte Gear { get; init; }
    public float Throttle { get; init; }
    public float Brake { get; init; }
    public bool Handbrake { get; init; }
    public float Steering { get; init; }
    public float Boost { get; init; }
    public float Fuel { get; init; }
    public float TireTempFrontLeft { get; init; }
    public float TireTempFrontRight { get; init; }
    public float TireTempRearLeft { get; init; }
    public float TireTempRearRight { get; init; }
    public ushort LapNumber { get; init; }
    public byte RacePosition { get; init; }
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

        var shift = horizon ? 12 : 0;
        var race = BinaryPrimitives.ReadInt32LittleEndian(packet);
        var maxRpm = F32(packet, 8);
        var idleRpm = F32(packet, 12);
        var rpm = F32(packet, 16);
        var lateral = F32(packet, 20);
        var longitudinal = F32(packet, 28);
        var speed = F32(packet, 244 + shift);
        var carOrdinal = BinaryPrimitives.ReadInt32LittleEndian(packet[212..]);
        var tireTempFrontLeft = F32(packet, 256 + shift);
        var tireTempFrontRight = F32(packet, 260 + shift);
        var tireTempRearLeft = F32(packet, 264 + shift);
        var tireTempRearRight = F32(packet, 268 + shift);
        var boost = F32(packet, 272 + shift);
        var fuel = F32(packet, 276 + shift);
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

        var steer = unchecked((sbyte)packet[308 + shift]);
        var frame = new TelemetryFrame
        {
            ReceivedTimestamp = Stopwatch.GetTimestamp(),
            GameTimestampMs = BinaryPrimitives.ReadUInt32LittleEndian(packet[4..]),
            RaceOn = race == 1,
            EngineMaxRpm = maxRpm,
            EngineIdleRpm = Sane(idleRpm, 0, 30_000) ? idleRpm : 0,
            EngineRpm = rpm,
            CarOrdinal = carOrdinal,
            LateralG = float.IsFinite(lateral) ? lateral / 9.80665f : 0,
            LongitudinalG = float.IsFinite(longitudinal) ? longitudinal / 9.80665f : 0,
            SpeedMps = speed,
            Boost = float.IsFinite(boost) ? boost : 0,
            Fuel = float.IsFinite(fuel) ? fuel : 0,
            TireTempFrontLeft = float.IsFinite(tireTempFrontLeft) ? tireTempFrontLeft : 0,
            TireTempFrontRight = float.IsFinite(tireTempFrontRight) ? tireTempFrontRight : 0,
            TireTempRearLeft = float.IsFinite(tireTempRearLeft) ? tireTempRearLeft : 0,
            TireTempRearRight = float.IsFinite(tireTempRearRight) ? tireTempRearRight : 0,
            LapNumber = BinaryPrimitives.ReadUInt16LittleEndian(packet[(300 + shift)..]),
            RacePosition = packet[302 + shift],
            Throttle = packet[303 + shift] / 255f,
            Brake = packet[304 + shift] / 255f,
            Handbrake = packet[306 + shift] != 0,
            Gear = packet[307 + shift],
            Steering = Math.Clamp(steer / 127f, -1, 1),
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
