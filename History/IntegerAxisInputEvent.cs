// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Mods.InputHistory.Modules;

namespace Celeste.Mods.InputHistory.History
{
    public class IntegerAxisInputEvent : InputEvent
    {
        private readonly InputHistoryModule module;
        public int X { get; }
        public int Y { get; }
        public bool MenuLeft { get; }
        public bool MenuRight { get; }
        public bool MenuUp { get; }
        public bool MenuDown { get; }

        public IntegerAxisInputEvent(InputHistoryModule module)
        {
            this.module = module;
            var input = module.Input;
            X = input.MoveX.Value;
            Y = input.MoveY.Value;
            if (module.MenuInputActive)
            {
                MenuLeft = input.MenuLeft.Check;
                MenuRight = input.MenuRight.Check;
                MenuUp = input.MenuUp.Check;
                MenuDown = input.MenuDown.Check;
            }
        }

        public float Render(ISourceRenderContext context, object owner, float x, float y, float fontSize)
        {
            var texture = module.Input.GuiDirection(new Vector2(X, Y));
            if (texture != null)
                SourceAtlasDrawing.Draw(context, owner, texture, new Vector2(x, y), Vector2.Zero,
                    module.GetColor(), fontSize / texture.FrameHeight);
            var right = module.Input.GuiDirection(Vector2.UnitX);
            return x + right.FrameWidth * fontSize / right.FrameHeight;
        }

        public bool Extends(InputEvent original, bool tas)
        {
            if (original is not IntegerAxisInputEvent other || X != other.X || Y != other.Y) return false;
            return !tas || (MenuLeft == other.MenuLeft && MenuRight == other.MenuRight &&
                            MenuUp == other.MenuUp && MenuDown == other.MenuDown);
        }

        public string ToTasString()
        {
            string result = "";
            if (X == -1 || MenuLeft) result += "L,";
            if (X == 1 || MenuRight) result += "R,";
            if (Y == -1 || MenuUp) result += "U,";
            if (Y == 1 || MenuDown) result += "D,";
            return result.Trim(',');
        }

        public bool HasInput() => X != 0 || Y != 0;
    }
}
