// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Startup;
using Celeste.Mods.InputHistory.Configuration;
using Celeste.Mods.InputHistory.History;
using Celeste.Mods.InputHistory.Modules;
using Celeste.Scene.Entities;

namespace Celeste.Mods.InputHistory.Presentation
{
    internal class InputHistoryListEntity : SceneEntityState
    {
        private readonly InputHistoryModule module;
        private HistoryEvent[] history = new HistoryEvent[InputHistorySettings.MAX_POSSIBLE_INPUTS_SHOWN];

        public InputHistoryListEntity(InputHistoryModule module)
            : base(((ISourceEntitySourceDataEnvironment)module.Engine).EntitySourceDataRegistration)
        {
            this.module = module;
            var tags = ((IOriginalGameTagsEnvironment)module.Engine).Tags;
            Tags = tags.HUD.Value | tags.Global.Value;
        }

        public override void Render(ISourceRenderContext context)
        {
            bool death = module.DeathOverride != InputHistoryModule.DeathOverrideState.INACTIVE;
            var settings = module.Settings;
            if (settings.ToggleVisibility?.Pressed == true)
            {
                settings.ToggleVisibility.Button.ConsumePress();
                settings.Visible = !settings.Visible;
            }

            if (!(settings.Visible ^ (settings.ToggleVisibilityHold?.Check == true)) && !death) return;
            int count = module.Events.Count;
            int shown = Math.Min(count, settings.MaxInputsShown);
            if (shown <= 0) return;
            if (history.Length < count) Array.Resize(ref history, count);
            module.Events.CopyTo(history, 0);
            float y = settings.VerticalPosition;
            try
            {
                for (int i = count - 1; i >= count - shown; i--)
                    y = history[i].Render(context, this, y, settings.ShowFrameCount || death);
            }
            finally
            {
                Array.Clear(history, 0, count);
            }
        }
    }
}
