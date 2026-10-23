using Celeste.Core.Reflection.Runtime;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.TheoKillBoxTrigger")]
    [CustomEntity("SJ2021/TheoKillBoxTrigger")]
    public sealed class TheoKillBoxTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;

        public TheoKillBoxTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
        }

        public override void Update()
        {
            if (((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible) return;
            var collisions = new List<TheoCrystalState>();
            foreach (TheoCrystalState crystal in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(TheoCrystalState)))
                if (SourceEntityCollision.Check(this, crystal))
                    collisions.Add(crystal);
            foreach (var crystal in collisions) crystal.Die();
        }
    }
}
