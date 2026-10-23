using System;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Components.Cassette
{
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.CassetteListener")]
    public class CassetteListener : SourceComponentState
    {
        public static PackedColor ColorFromCassetteIndex(int index) => index switch
        {
            0 => new PackedColor(73, 170, 240), 1 => new PackedColor(240, 73, 190),
            2 => new PackedColor(252, 220, 58), 3 => new PackedColor(56, 224, 78), _ => PackedColor.White
        };

        public Action OnFinish, OnWillToggle, OnActivated, OnDeactivated;
        public Action<bool> OnSilentUpdate;
        public Action<int, bool> OnTick;
        public int Index;
        public bool Activated;
        private OriginalCassetteBlockManagerState cassetteBlockManager;

        public CassetteListener(int index) : base(false, false)
        {
            Index = index;
        }

        public override void EntityAdded(object scene)
        {
            base.EntityAdded(scene);
            if (scene is not OriginalLevelSceneRoot level) return;
            level.HasCassetteBlocks = true;
            var scheduler = ((ISourceScene)scene).SourceScene;
            cassetteBlockManager =
                scheduler.Entities.GetTrackedEntity<OriginalCassetteBlockManagerState>("Celeste.CassetteBlockManager");
            if (cassetteBlockManager == null)
                foreach (var pending in scheduler.GetToAdd())
                    if (pending is OriginalCassetteBlockManagerState manager)
                    {
                        cassetteBlockManager = manager;
                        break;
                    }

            if (cassetteBlockManager == null)
                scheduler.QueueSourceEntity(cassetteBlockManager =
                    ((IOriginalCassetteManagerEnvironment)level.EngineContext).CassetteManagers.Create());
        }

        public override void EntityRemoved(object scene)
        {
            base.EntityRemoved(scene);
            cassetteBlockManager = null;
        }
    }
}
