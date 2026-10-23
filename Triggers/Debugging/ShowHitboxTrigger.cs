using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.StrawberryJam.Runtime.Debugging;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.StrawberryJam.Triggers.Debugging
{
    [CustomEntity("SJ2021/ShowHitboxTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Triggers.ShowHitboxTrigger")]
    [SceneTracked(false)]
    public class ShowHitboxTrigger : SourceTriggerEntityState
    {
        private readonly ShowHitboxRuntime runtime;

        public static HashSet<string> EnabledTypeNames => ShowHitboxRuntime
            .For(Celeste.Game.Execution.EngineRuntime.Instance.Process).EnabledTypeNames;

        public HashSet<string> TypeNames { get; }

        public ShowHitboxTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            runtime = ShowHitboxRuntime.For(engine);
            runtime.BindDraw();
            TypeNames = new HashSet<string>(data.Attr("typeNames")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(str => str.Trim()));
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            runtime.Refresh();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            runtime.Clear();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            runtime.Refresh();
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            runtime.Refresh();
        }
    }
}
