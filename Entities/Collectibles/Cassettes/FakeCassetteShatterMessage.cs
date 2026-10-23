// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Runtime.Startup;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Runtime;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles;
using Celeste.Graphics.Particles;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.StrawberryJam.Entities.Collectibles.Cassettes
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FakeCassette+UnlockedBSide")]
    internal class FakeCassetteShatterMessage : SceneEntityState
    {
        private readonly GameApplication engine;

        public FakeCassetteShatterMessage(ISourceEngineContext engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = (GameApplication)engine;
        }

        public Sprite sprite;

        private float timer;
        private bool shaking;
        private Vector2 shakeVector;
        private float shakeTimer;

        public override void Added(object scene)
        {
            base.Added(scene);
            base.Tags = ((IEverestGameTagsEnvironment)engine).EverestTags.SubHUD.Value |
                        ((IOriginalGameTagsEnvironment)engine).Tags.PauseUpdate.Value;
            base.Depth = -10000;
            sprite = new Sprite(engine,
                ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).GuiAtlas,
                "StrawberryJam2021/fakeCassette/shatter");
            sprite.Add("shatter", "", 0.07f, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                21, 22); //this wasn't working for some reason so this was my fix
            sprite.Rate = 0f;
            sprite.Play("shatter");
            sprite.Justify = new Vector2(0.5f, 1f);
        }

        public IEnumerator EaseIn()
        {
            _ = Scene;
            OriginalLevelSceneRoot level = Scene as OriginalLevelSceneRoot;
            level.FormationBackdrop.Display = true;
            while ((level.FormationBackdrop.Alpha += engine.DeltaTime / 0.75f) < 1f)
            {
                yield return null;
            }

            level.FormationBackdrop.Alpha = 1;
            yield return 0.5f;
        }


        public override void Update()
        {
            timer += engine.DeltaTime;
            base.Update();
            sprite.Update();
            if (shaking)
            {
                shakeTimer += engine.DeltaTime;
                //magnitude of the shake vector determined by a cube root function, because they ramp up slowly and cleanly and have f(1) = 1 which lets us use a different function for t < 1 and t > 1
                //use linear function for values of time below 1, cube root function ramps too fast.
                shakeVector = 0.01F * RandomShakeVector.Next(engine.Random) *
                              (shakeTimer > 1 ? (float)Math.Pow(shakeTimer, 0.33) : shakeTimer);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            float num = engine.EasingArithmetic.CubeOut((Scene as OriginalLevelSceneRoot).FormationBackdrop.Alpha);
            Vector2 vector = GameRuntime.TargetCenter + new Vector2(0f, 64f);
            Vector2 vector2 = Vector2.UnitY * 64f * (1f - num);
            SourceAtlasDrawing.DrawJustified(context, this, sprite.Texture, vector - vector2 + new Vector2(0f, 32f),
                new Vector2(0.5f + shakeVector.X, 0.75f + shakeVector.Y), PackedColor.White * num, 1.5f);
        }

        public void Shake()
        {
            shaking = true;
        }

        public void StopShake()
        {
            shaking = false;
            shakeTimer = 0;
            shakeVector = Vector2.Zero;
        }
    }
}
