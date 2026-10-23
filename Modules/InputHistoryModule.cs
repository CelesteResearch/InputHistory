// Modified for the shared runtime: input, rendering, and lifecycle integration.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Scheduling.Hooks;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Runtime.Chapters.Flow;
using Celeste.Everest.Runtime.Events;
using Celeste.Everest.Runtime.Events.Player;
using Celeste.Everest.Runtime.Startup;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Engine.Update;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Environment;
using Celeste.Input.Runtime;
using Celeste.Mods.InputHistory.Configuration;
using Celeste.Mods.InputHistory.History;
using Celeste.Mods.InputHistory.IO;
using Celeste.Mods.InputHistory.Presentation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.InputHistory.Modules
{
    public class InputHistoryModule : SourceEverestModule
    {
        public enum DeathOverrideState
        {
            INACTIVE,
            FORCED,
            WAITING
        }

        public Queue<HistoryEvent> Events { get; } = new Queue<HistoryEvent>();
        private DeathOverrideState deathOverride;
        private QueuedStreamWriter replayWriter;
        private HistoryEvent lastReplayEvent;
        private bool onEnter;
        public override Type SettingsType => typeof(InputHistorySettings);
        public InputHistorySettings Settings => (InputHistorySettings)_Settings;
        public ISourceEngineContext Engine => SourceEngine;
        public OriginalGameInputProcess Input => ((IOriginalGameInputProcessEnvironment)Engine).InputSource;
        public SourceMInputProcess MInput => ((ISourceMInputEnvironment)Engine).MInput;
        public OriginalGameTextProcess Text => ((IOriginalGameTextEnvironment)Engine).Text;

        public bool MenuInputActive => Engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level &&
                                       (level.Paused || level.WasPaused);

        public DeathOverrideState DeathOverride =>
            Settings.TemporarilyShowOnDeath ? deathOverride : DeathOverrideState.INACTIVE;

        private EverestGameEventsProcess GameEvents => ((IEverestGameEventsEnvironment)Engine).GameEvents;
        private EverestLevelEnterEvents EnterEvents => ((IEverestLevelEnterEventsEnvironment)Engine).LevelEnterEvents;

        private SourceEngineUpdateHooks EngineHooks =>
            ((ISourceUpdateMethodHooksEnvironment)Engine).SourceEngineUpdateHooks;

        public InputHistoryModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public PackedColor GetColor() => PackedColor.White * (Settings.Opacity / 100f);

        public override void Load()
        {
            GameEvents.OnLoadLevel += AddList;
            EnterEvents.OnEnter += OnEnter;
            GameEvents.OnExit += OnExit;
            EverestPlayerSourceEvents.OnDie += OnDie;
            EngineHooks.AfterUpdate += UpdateList;
        }

        private void OnEnter(ChapterCompletionSession session, bool fromSaveData)
        {
            onEnter = true;
            if (!Settings.EnableReplays) return;
            string directory = Path.Combine(((IEverestGamePathEnvironment)Engine).PathGame, "InputHistoryReplays");
            Directory.CreateDirectory(directory);
            var areaKeys = ((IOriginalAreaKeyEnvironment)Engine).AreaKeys;
            string sid = areaKeys.GetSID(areaKeys.GetSessionArea(session));
            string name = sid.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
            string command = "console ";
            if (session.Mode == 0) command += "load";
            else if (session.Mode == 1)
            {
                command += "hard";
                name += "_B";
            }
            else if (session.Mode == 2)
            {
                command += "rmx2";
                name += "_C";
            }

            replayWriter = new QueuedStreamWriter(Path.Combine(directory,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_") + name + ".tas"));
            if (fromSaveData && session.RespawnPoint.HasValue)
                replayWriter.WriteLineQueued(
                    $"{command} {sid} {session.RespawnPoint.Value.X} {session.RespawnPoint.Value.Y} 0 0");
            else replayWriter.WriteLineQueued(command + " " + sid);
        }

        private void OnExit(OriginalLevelSceneRoot level, LevelExitSequence exit, LevelExitMode mode,
            ChapterCompletionSession session, HiresSnowState snow)
        {
            WriteOutLastEvent();
            Events.Clear();
            lastReplayEvent = null;
            deathOverride = DeathOverrideState.INACTIVE;
            if (mode != LevelExitMode.Restart && mode != LevelExitMode.GoldenBerryRestart)
            {
                replayWriter?.CloseQueued();
                replayWriter = null;
            }
        }

        private void OnDie(RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, Engine)) deathOverride = DeathOverrideState.FORCED;
        }

        private void WriteOutLastEvent()
        {
            if (lastReplayEvent == null || replayWriter == null) return;
            if (!Settings.EnableReplays)
            {
                replayWriter.CloseQueued();
                replayWriter = null;
            }

            replayWriter?.WriteLineQueued(lastReplayEvent.ToTasString());
        }

        private void UpdateList()
        {
            if (onEnter || Engine is IOriginalEngineUpdateHooks { PausedAfterInput: true }) return;
            if (deathOverride == DeathOverrideState.FORCED && Engine.Frame.Scenes.CurrentScene is ISourceScene scene)
            {
                var player = scene.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null && !player.Dead) deathOverride = DeathOverrideState.WAITING;
            }

            var current = HistoryEvent.CreateDefaultHistoryEvent(this);
            if (deathOverride == DeathOverrideState.WAITING && current.HasInput())
                deathOverride = DeathOverrideState.INACTIVE;
            if (!current.Extends(Events.LastOrDefault(), false))
            {
                Events.Enqueue(current);
                while (Events.Count > InputHistorySettings.MAX_POSSIBLE_INPUTS_SHOWN) Events.Dequeue();
            }
            else
            {
                var last = Events.Last();
                last.Time += current.Time;
                last.Frames++;
            }

            if (!Settings.EnableReplays)
            {
                WriteOutLastEvent();
                return;
            }

            var replay = HistoryEvent.CreateTasHistoryEvent(this);
            if (replay.Extends(lastReplayEvent, true))
            {
                lastReplayEvent.Time += current.Time;
                lastReplayEvent.Frames++;
            }
            else
            {
                WriteOutLastEvent();
                lastReplayEvent = replay;
            }
        }

        private void AddList(OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            if (onEnter)
            {
                onEnter = false;
                Events.Clear();
                lastReplayEvent = null;
            }

            if (fromLoader) ((ISourceScene)level).SourceScene.QueueSourceEntity(new InputHistoryListEntity(this));
            replayWriter?.WriteLineQueued("# " + level.Session.LevelData.Name);
            replayWriter?.FlushQueued();
        }

        public override void Unload()
        {
            GameEvents.OnLoadLevel -= AddList;
            EnterEvents.OnEnter -= OnEnter;
            GameEvents.OnExit -= OnExit;
            EverestPlayerSourceEvents.OnDie -= OnDie;
            EngineHooks.AfterUpdate -= UpdateList;
            replayWriter?.CloseQueued();
            replayWriter = null;
        }
    }
}
