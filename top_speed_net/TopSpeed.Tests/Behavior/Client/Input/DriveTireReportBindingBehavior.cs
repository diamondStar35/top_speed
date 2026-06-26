using TopSpeed.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class DriveTireReportBindingBehaviorTests
{
    [Fact]
    public void ReportTireState_DefaultKeyboardBinding_ShouldUseB_AndNotOverrideGearDown()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Keyboard };
        var input = new DriveInput(settings);

        settings.GetKeyboardBinding(DriveIntent.ReportTireState).Should().Be(InputKey.B);
        settings.GetKeyboardBinding(DriveIntent.GearDown).Should().Be(InputKey.Z);

        input.Run(new InputState(), 0f);
        var state = new InputState();
        state.Set(InputKey.B, true);
        input.Run(state, 0f);

        input.Intents.IsTriggered(DriveIntent.ReportTireState).Should().BeTrue();
        input.Intents.IsTriggered(DriveIntent.GearDown).Should().BeFalse();
    }

    [Fact]
    public void ReportTireState_RemappedKeyboardBinding_ShouldTriggerNewKeyOnly()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Keyboard };
        var input = new DriveInput(settings);
        input.SetReportTireState(InputKey.F10);

        input.Run(new InputState(), 0f);
        var oldKey = new InputState();
        oldKey.Set(InputKey.B, true);
        input.Run(oldKey, 0f);
        input.Intents.IsTriggered(DriveIntent.ReportTireState).Should().BeFalse();

        input.Run(new InputState(), 0f);
        var newKey = new InputState();
        newKey.Set(InputKey.F10, true);
        input.Run(newKey, 0f);
        input.Intents.IsTriggered(DriveIntent.ReportTireState).Should().BeTrue();
    }
}
