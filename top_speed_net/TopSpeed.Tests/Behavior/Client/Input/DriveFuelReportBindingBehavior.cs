using TopSpeed.Input;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class DriveFuelReportBindingBehaviorTests
{
    [Fact]
    public void ReportFuel_DefaultKeyboardBinding_ShouldTriggerIntent()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Keyboard };
        var input = new DriveInput(settings);

        settings.GetKeyboardBinding(DriveIntent.ReportFuel).Should().Be(InputKey.X);

        input.Run(new InputState(), 0f);
        var state = new InputState();
        state.Set(InputKey.X, true);
        input.Run(state, 0f);

        input.Intents.IsTriggered(DriveIntent.ReportFuel).Should().BeTrue();
    }

    [Fact]
    public void ReportFuel_RemappedKeyboardBinding_ShouldTriggerNewKeyOnly()
    {
        var settings = new DriveSettings { DeviceMode = InputDeviceMode.Keyboard };
        var input = new DriveInput(settings);
        input.SetReportFuel(InputKey.F10);

        input.Run(new InputState(), 0f);
        var oldKey = new InputState();
        oldKey.Set(InputKey.X, true);
        input.Run(oldKey, 0f);
        input.Intents.IsTriggered(DriveIntent.ReportFuel).Should().BeFalse();

        input.Run(new InputState(), 0f);
        var newKey = new InputState();
        newKey.Set(InputKey.F10, true);
        input.Run(newKey, 0f);
        input.Intents.IsTriggered(DriveIntent.ReportFuel).Should().BeTrue();
    }
}
