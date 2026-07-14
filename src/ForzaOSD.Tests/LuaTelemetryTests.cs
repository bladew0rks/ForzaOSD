using ForzaOSD.App;
using ForzaOSD.Core;
using KeraLua;

namespace ForzaOSD.Tests;

public sealed class LuaTelemetryTests
{
    private static readonly string[] CompleteFields =
    [
        "available",
        "fresh",
        "race_on",
        "rpm",
        "max_rpm",
        "idle_rpm",
        "acceleration_x",
        "acceleration_y",
        "acceleration_z",
        "velocity_x",
        "velocity_y",
        "velocity_z",
        "angular_velocity_x",
        "angular_velocity_y",
        "angular_velocity_z",
        "yaw",
        "pitch",
        "roll",
        "normalized_suspension_travel_front_left",
        "normalized_suspension_travel_front_right",
        "normalized_suspension_travel_rear_left",
        "normalized_suspension_travel_rear_right",
        "tire_slip_ratio_front_left",
        "tire_slip_ratio_front_right",
        "tire_slip_ratio_rear_left",
        "tire_slip_ratio_rear_right",
        "wheel_rotation_speed_front_left",
        "wheel_rotation_speed_front_right",
        "wheel_rotation_speed_rear_left",
        "wheel_rotation_speed_rear_right",
        "wheel_on_rumble_strip_front_left",
        "wheel_on_rumble_strip_front_right",
        "wheel_on_rumble_strip_rear_left",
        "wheel_on_rumble_strip_rear_right",
        "wheel_in_puddle_front_left",
        "wheel_in_puddle_front_right",
        "wheel_in_puddle_rear_left",
        "wheel_in_puddle_rear_right",
        "surface_rumble_front_left",
        "surface_rumble_front_right",
        "surface_rumble_rear_left",
        "surface_rumble_rear_right",
        "tire_slip_angle_front_left",
        "tire_slip_angle_front_right",
        "tire_slip_angle_rear_left",
        "tire_slip_angle_rear_right",
        "tire_combined_slip_front_left",
        "tire_combined_slip_front_right",
        "tire_combined_slip_rear_left",
        "tire_combined_slip_rear_right",
        "suspension_travel_meters_front_left",
        "suspension_travel_meters_front_right",
        "suspension_travel_meters_rear_left",
        "suspension_travel_meters_rear_right",
        "car_ordinal",
        "car_class",
        "car_performance_index",
        "drivetrain_type",
        "num_cylinders",
        "car_group",
        "smashable_velocity_difference",
        "smashable_mass",
        "position_x",
        "position_y",
        "position_z",
        "speed_mps",
        "speed_kph",
        "speed_mph",
        "power_watts",
        "torque_nm",
        "tire_temp_front_left",
        "tire_temp_front_right",
        "tire_temp_rear_left",
        "tire_temp_rear_right",
        "boost",
        "fuel",
        "distance_traveled_m",
        "best_lap_seconds",
        "last_lap_seconds",
        "current_lap_seconds",
        "race_time_seconds",
        "lap_number",
        "race_position",
        "gear",
        "gear_label",
        "throttle",
        "brake",
        "clutch",
        "handbrake",
        "steering_raw",
        "steering",
        "driving_line_raw",
        "driving_line",
        "ai_brake_difference_raw",
        "ai_brake_difference",
        "lateral_g",
        "longitudinal_g",
    ];

    [Fact]
    public void CompleteTelemetrySurfaceIsAvailableToLua()
    {
        using var lua = new Lua(true);
        var snapshot = new TelemetrySnapshot
        {
            HasFrame = true,
            Stale = false,
            Frame = new TelemetryFrame
            {
                AccelerationX = 1.25f,
                TireCombinedSlipRearRight = -1.5f,
                WheelInPuddleFrontLeft = true,
                CarGroup = 42,
                SpeedMps = 10,
                Clutch = .5f,
                SteeringRaw = -64,
                DrivingLineRaw = 63,
                AiBrakeDifferenceRaw = -127,
                Gear = 4,
            },
        };

        HudRuntime.PushTelemetry(lua, snapshot);
        lua.SetGlobal("telemetry");

        foreach (var field in CompleteFields)
        {
            lua.GetGlobal("telemetry");
            lua.GetField(-1, field);
            Assert.False(lua.IsNil(-1), $"Missing telemetry.{field}");
            lua.Pop(2);
        }

        Assert.Equal(1.25, Number(lua, "acceleration_x"));
        Assert.Equal(-1.5, Number(lua, "tire_combined_slip_rear_right"));
        Assert.Equal(36, Number(lua, "speed_kph"), 5);
        Assert.Equal(42, Number(lua, "car_group"));
        Assert.Equal(-64, Number(lua, "steering_raw"));
        Assert.Equal("4", String(lua, "gear_label"));
        Assert.True(Boolean(lua, "wheel_in_puddle_front_left"));
    }

    [Fact]
    public void MissingFrameUsesNeutralGear()
    {
        using var lua = new Lua(true);
        HudRuntime.PushTelemetry(lua, new());
        lua.SetGlobal("telemetry");
        Assert.Equal(11, Number(lua, "gear"));
        Assert.Equal("N", String(lua, "gear_label"));
    }

    private static double Number(Lua lua, string field)
    {
        lua.GetGlobal("telemetry");
        lua.GetField(-1, field);
        var value = lua.ToNumber(-1);
        lua.Pop(2);
        return value;
    }

    private static string String(Lua lua, string field)
    {
        lua.GetGlobal("telemetry");
        lua.GetField(-1, field);
        var value = lua.ToString(-1);
        lua.Pop(2);
        return value;
    }

    private static bool Boolean(Lua lua, string field)
    {
        lua.GetGlobal("telemetry");
        lua.GetField(-1, field);
        var value = lua.ToBoolean(-1);
        lua.Pop(2);
        return value;
    }
}
