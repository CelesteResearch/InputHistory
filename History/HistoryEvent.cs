// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Input.Devices.Keyboard;
using Celeste.Mods.InputHistory.Modules;

namespace Celeste.Mods.InputHistory.History
{
    public class HistoryEvent
    {
        private readonly InputHistoryModule module;
        private readonly List<InputEvent> events;
        private long frames;
        private string frameCount;
        public float Time { get; set; }

        public long Frames
        {
            get => frames;
            set
            {
                frames = value;
                frameCount = null;
            }
        }

        private HistoryEvent(InputHistoryModule module, List<InputEvent> events)
        {
            this.module = module;
            this.events = events;
            Time = module.Engine.RawDeltaTime;
            Frames = 1;
        }

        public static HistoryEvent CreateDefaultHistoryEvent(InputHistoryModule module) =>
            new(module, DefaultEventList(module));

        public static HistoryEvent CreateTasHistoryEvent(InputHistoryModule module)
        {
            var events = DefaultEventList(module);
            var input = module.Input;
            events.Add(new ButtonInputEvent(module, input.Pause, SourceKeys.S));
            events.Add(new ButtonInputEvent(module, input.QuickRestart, SourceKeys.R));
            events.Add(new ButtonInputEvent(module, input.MenuJournal, SourceKeys.N));
            events.Add(new ButtonInputEvent(module, input.Talk, SourceKeys.N));
            events.Add(new ButtonInputEvent(module, input.MenuConfirm, SourceKeys.O));
            return new HistoryEvent(module, events);
        }

        private static List<InputEvent> DefaultEventList(InputHistoryModule module)
        {
            var input = module.Input;
            return new List<InputEvent>
            {
                new IntegerAxisInputEvent(module),
                new ButtonInputEvent(module, input.Jump, SourceKeys.J),
                new MultiButtonInputEvent(module,
                    new ButtonInputEvent(module, input.Dash, SourceKeys.X),
                    new ButtonInputEvent(module, input.CrouchDash, SourceKeys.Z)),
                new ButtonInputEvent(module, input.Grab, SourceKeys.G)
            };
        }

        public float Render(ISourceRenderContext context, object owner, float y, bool showFrameCount)
        {
            float fontSize = module.Text.ActiveFont.LineHeight * .5f;
            float x = module.Settings.HorizontalPosition;
            foreach (var item in events) x = item.Render(context, owner, x, y, fontSize);
            if (showFrameCount)
                module.Text.ActiveFont.DrawOutline(context, owner,
                    Frames < 0 ? Frames.ToString() : frameCount ??= Frames.ToString(), new Vector2(x, y), Vector2.Zero,
                    new Vector2(.5f), module.GetColor(), 2f, PackedColor.Black);
            return y + fontSize;
        }

        public bool Extends(HistoryEvent other, bool tas)
        {
            if (other == null || events.Count != other.events.Count) return false;
            bool extends = true;
            for (int i = 0; i < events.Count; i++) extends = events[i].Extends(other.events[i], tas) && extends;
            return extends;
        }

        public string ToTasString()
        {
            string result = Frames.ToString();
            foreach (var item in events)
            {
                string value = item.ToTasString();
                if (value != "") result += "," + value;
            }

            return result;
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
