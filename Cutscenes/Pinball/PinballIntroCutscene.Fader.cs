using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Scene.Transitions;

namespace Celeste.Mods.StrawberryJam.Cutscenes.Pinball
{
    internal partial class PinballIntroCutscene
    {
        [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PinballIntroCutscene+Fader")]
        private class Fader : SceneEntityState
        {
            private readonly GameApplication engine;
            public float Target;
            public bool Ended;
            private float fade;

            public Fader(GameApplication engine) : base(engine.EntitySourceDataRegistration)
            {
                this.engine = engine;
                Depth = -1000000;
            }

            public override void Update()
            {
                fade = ScalarMath.Approach(fade, Target, engine.DeltaTime * .5f);
                if (Target <= 0f && fade <= 0f && Ended) RemoveSelf();
                base.Update();
            }

            public override void Render(ISourceRenderContext context)
            {
                var camera = ((OriginalLevelSceneRoot)Scene).Camera;
                if (fade > 0f)
                    SourcePrimitiveDrawing.Rect(context, this, camera.Position.X - 10f, camera.Position.Y - 10f, 340f,
                        200f, PackedColor.Black * fade);
                var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null && !player.OnGround(2)) player.Render(context);
            }
        }
    }
}
