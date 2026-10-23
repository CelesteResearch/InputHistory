// Modified for the shared runtime: input, rendering, and lifecycle integration.

using Celeste.Everest.Input.Bindings;
using Celeste.Everest.Modules.Settings;
using Celeste.Everest.Settings.Metadata;

namespace Celeste.Mods.InputHistory.Configuration
{
    public class InputHistorySettings : EverestModuleSettings
    {
        public const int MAX_POSSIBLE_INPUTS_SHOWN = 40;

        public bool Visible { get; set; } = true;
        public bool ShowFrameCount { get; set; } = true;
        public bool TemporarilyShowOnDeath { get; set; }

        [SettingRange(0, 40)] public int MaxInputsShown { get; set; } = 25;

        [SettingRange(0, 1920, true)]
        [SettingSubText("Default value: 10")]
        public int HorizontalPosition { get; set; } = 10;

        [SettingRange(0, 1080, true)]
        [SettingSubText("Default value: 120")]
        public int VerticalPosition { get; set; } = 120;

        [SettingRange(0, 100, true)] public int Opacity { get; set; } = 100;

        [SettingSubText("Experimental! Won't start recording until next level load.")]
        public bool EnableReplays { get; set; }

        public EverestButtonBinding ToggleVisibility { get; set; }
        public EverestButtonBinding ToggleVisibilityHold { get; set; }
    }
}
