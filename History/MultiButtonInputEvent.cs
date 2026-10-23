// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System;
using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Mods.InputHistory.Modules;

namespace Celeste.Mods.InputHistory.History
{
    public class MultiButtonInputEvent : InputEvent
    {
        private readonly InputHistoryModule module;
        private readonly ButtonInputEvent[] events;

        public MultiButtonInputEvent(InputHistoryModule module, params ButtonInputEvent[] events)
        {
            this.module = module;
            this.events = events;
        }

        public float Render(ISourceRenderContext context, object owner, float x, float y, float fontSize)
        {
            int count = 0;
            foreach (var item in events) count += item.Check;
            float right = x;
            float scale = 5f / (5 + count - 1);
            int index = 0;
            foreach (var item in events)
            {
                var texture = module.Input.GuiKey(item.Key);
                right = Math.Max(right, x + texture.FrameWidth * fontSize / texture.FrameHeight);
                for (int i = 0; i < item.Check; i++, index++)
                {
                    var offset = new Vector2(
                        texture.FrameWidth * fontSize / texture.FrameHeight * index / (5f + count - 1f),
                        fontSize * (count - index - 1) / (5f + count - 1f));
                    SourceAtlasDrawing.Draw(context, owner, texture, new Vector2(x, y) + offset, Vector2.Zero,
                        module.GetColor(), fontSize / texture.FrameHeight * scale);
                }
            }

            return right;
        }

        public bool Extends(InputEvent original, bool tas)
        {
            if (original is not MultiButtonInputEvent other || events.Length != other.events.Length) return false;
            bool extends = true;
            for (int i = 0; i < events.Length; i++) extends = events[i].Extends(other.events[i], tas) && extends;
            return extends;
        }

        public string ToTasString()
        {
            string result = "";
            foreach (var item in events)
            {
                string value = item.ToTasString();
                if (value != "") result += value + ",";
            }

            return result.Trim(',');
        }

        public bool HasInput()
        {
            foreach (var item in events)
                if (item.HasInput())
                    return true;
            return false;
        }
    }
}
