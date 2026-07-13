using System.Runtime.InteropServices;
using ForzaOSD.Core;

namespace ForzaOSD.App;

internal enum MediaCommand
{
    PreviousTrack,
    NextTrack,
    TogglePlayPause,
}

internal sealed class DPadMediaBinding
{
    internal const int ChordWindowMilliseconds = 120;
    private MediaCommand? pending;
    private long pendingSince;
    private bool previousLeft;
    private bool previousRight;
    private bool chordLatched;

    internal MediaCommand? Update(bool left, bool right, long timestamp)
    {
        var leftPressed = left && !previousLeft;
        var rightPressed = right && !previousRight;
        previousLeft = left;
        previousRight = right;

        if (left && right)
        {
            pending = null;
            if (!chordLatched)
            {
                chordLatched = true;
                return MediaCommand.TogglePlayPause;
            }
            return null;
        }

        if (!left && !right)
            chordLatched = false;

        if (!chordLatched)
        {
            if (leftPressed)
            {
                pending = MediaCommand.PreviousTrack;
                pendingSince = timestamp;
            }
            else if (rightPressed)
            {
                pending = MediaCommand.NextTrack;
                pendingSince = timestamp;
            }
        }

        if (
            pending is { } command
            && timestamp - pendingSince >= ChordWindowMilliseconds
        )
        {
            pending = null;
            return command;
        }
        return null;
    }

    internal void Reset()
    {
        pending = null;
        previousLeft = false;
        previousRight = false;
        chordLatched = false;
    }
}

internal sealed class GamepadMediaControls
{
    private const ushort DPadLeft = 0x0004;
    private const ushort DPadRight = 0x0008;
    private readonly DPadMediaBinding binding = new();
    private long lastPoll;
    private int activeController = -1;

    internal string Status { get; private set; } = "Waiting for an XInput controller";

    internal MediaCommand? Poll(AudioConfig config, bool isDriving)
    {
        if (!config.MediaControlsEnabled)
        {
            Reset("D-pad media controls disabled");
            return null;
        }

        if (!isDriving)
        {
            Reset("Inactive while Forza is not in active driving");
            return null;
        }

        var now = Environment.TickCount64;
        if (now - lastPoll < 8)
            return null;
        lastPoll = now;

        if (!TryGetController(config.GamepadIndex, out var index, out var state))
        {
            Reset("No XInput controller detected");
            return null;
        }

        if (index != activeController)
        {
            binding.Reset();
            activeController = index;
        }
        Status = $"Controller {index + 1}";
        var buttons = state.Gamepad.Buttons;
        return binding.Update(
            (buttons & DPadLeft) != 0,
            (buttons & DPadRight) != 0,
            now
        );
    }

    private bool TryGetController(int configured, out int index, out XInputState state)
    {
        if (configured is >= 0 and <= 3)
        {
            index = configured;
            return XInputGetState((uint)configured, out state) == 0;
        }

        if (activeController is >= 0 and <= 3)
        {
            index = activeController;
            if (XInputGetState((uint)activeController, out state) == 0)
                return true;
        }

        for (index = 0; index < 4; index++)
            if (XInputGetState((uint)index, out state) == 0)
                return true;

        index = -1;
        state = default;
        return false;
    }

    private void Reset(string status)
    {
        binding.Reset();
        activeController = -1;
        Status = status;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState(uint userIndex, out XInputState state);

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        internal uint PacketNumber;
        internal XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        internal ushort Buttons;
        internal byte LeftTrigger;
        internal byte RightTrigger;
        internal short LeftThumbX;
        internal short LeftThumbY;
        internal short RightThumbX;
        internal short RightThumbY;
    }
}
