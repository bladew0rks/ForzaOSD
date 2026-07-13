using ForzaOSD.App;

namespace ForzaOSD.Tests;

public sealed class GamepadMediaControlsTests
{
    [Fact]
    public void RightPressSkipsOnceAfterChordWindow()
    {
        var binding = new DPadMediaBinding();

        Assert.Null(binding.Update(false, true, 0));
        Assert.Null(binding.Update(false, true, 100));
        Assert.Equal(MediaCommand.NextTrack, binding.Update(false, true, 120));
        Assert.Null(binding.Update(false, true, 500));
        Assert.Null(binding.Update(false, false, 510));
    }

    [Fact]
    public void LeftPressGoesToPreviousTrack()
    {
        var binding = new DPadMediaBinding();

        Assert.Null(binding.Update(true, false, 0));
        Assert.Equal(MediaCommand.PreviousTrack, binding.Update(true, false, 120));
    }

    [Fact]
    public void LeftAndRightChordTogglesWithoutSkipping()
    {
        var binding = new DPadMediaBinding();

        Assert.Null(binding.Update(true, false, 0));
        Assert.Equal(MediaCommand.TogglePlayPause, binding.Update(true, true, 60));
        Assert.Null(binding.Update(true, true, 500));
        Assert.Null(binding.Update(false, false, 510));
    }

    [Fact]
    public void ResetDiscardsAPendingDrivingInput()
    {
        var binding = new DPadMediaBinding();

        Assert.Null(binding.Update(false, true, 0));
        binding.Reset();

        Assert.Null(binding.Update(false, false, 500));
    }
}
