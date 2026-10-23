// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Everest.Input;
using Celeste.Graphics.Rendering;
using Celeste.Input.Bindings;
using Celeste.Input.Devices.Keyboard;
using Celeste.Mods.InputHistory.Modules;

namespace Celeste.Mods.InputHistory.History
{
    public class ButtonInputEvent : InputEvent
    {
        private readonly InputHistoryModule module;
        private readonly SourceVirtualButton button;
        private int tasButtonIndex;
        public int Check { get; }
        public bool MenuPress { get; }
        public bool Pressed { get; }
        public SourceKeys Key { get; }

        public ButtonInputEvent(InputHistoryModule module, SourceVirtualButton button, SourceKeys key)
        {
            this.module = module;
            this.button = button;
            Key = key;
            Check = CheckCount(module, button);
            Pressed = button.Binding.Pressed(module.MInput, button.GamepadIndex, button.Threshold);
            tasButtonIndex = Check > 0 ? 0 : -1;
            if (module.MenuInputActive && button == module.Input.Dash)
                MenuPress = module.Input.MenuCancel.Pressed;
        }

        public float Render(ISourceRenderContext context, object owner, float x, float y, float fontSize)
        {
            var texture = module.Input.GuiKey(Key);
            float scale = 5f / (5 + Check - 1);
            for (int i = 0; i < Check; i++)
            {
                var offset = new Vector2(texture.FrameWidth * fontSize / texture.FrameHeight * i / (5f + Check - 1f),
                    fontSize * (Check - i - 1) / (5f + Check - 1f));
                SourceAtlasDrawing.Draw(context, owner, texture, new Vector2(x, y) + offset, Vector2.Zero,
                    module.GetColor(), fontSize / texture.FrameHeight * scale);
            }

            return x + texture.FrameWidth * fontSize / texture.FrameHeight;
        }

        public bool Extends(InputEvent original, bool tas)
        {
            if (original is not ButtonInputEvent other) return false;
            if (!tas) return !Pressed && Check == other.Check;
            tasButtonIndex = other.tasButtonIndex;
            if (Pressed || MenuPress) tasButtonIndex = (tasButtonIndex + 1) % 2;
            else if (Check == 0) tasButtonIndex = -1;
            return ToTasString() == other.ToTasString();
        }

        public string ToTasString()
        {
            if (Check == 0 && !MenuPress) return "";
            var input = module.Input;
            if (button == input.Jump) return tasButtonIndex == 0 ? "J" : tasButtonIndex == 1 ? "K" : "";
            if (button == input.Dash) return tasButtonIndex == 0 ? "C" : tasButtonIndex == 1 ? "X" : "";
            if (button == input.CrouchDash) return tasButtonIndex == 0 ? "Z" : tasButtonIndex == 1 ? "V" : "";
            if (button == input.Grab) return tasButtonIndex == 0 ? "G" : tasButtonIndex == 1 ? "H" : "";
            if (button == input.Pause) return "S";
            if (button == input.QuickRestart) return "Q";
            if (button == input.MenuJournal || button == input.Talk) return "N";
            return button == input.MenuConfirm ? "O" : "";
        }

        private static int CheckCount(InputHistoryModule module, SourceVirtualButton button)
        {
            int count = 0;
            foreach (var key in button.Binding.Keyboard)
                if (module.MInput.Keyboard.Check(key))
                    count++;
            if (button == module.Input.Pause && module.Input.ESC.Check) count++;
            foreach (var padButton in button.Binding.Controller)
                if (module.MInput.GamePads[button.GamepadIndex].Check(padButton, button.Threshold))
                    count++;
            if (button.Binding is InstalledSourceInputBinding binding)
                foreach (var mouseButton in binding.Mouse)
                    if (((SourceEverestMouseData)module.MInput.Mouse).Check(mouseButton))
                        count++;
            return count;
        }

        public bool HasInput() => Check != 0;
    }
}
