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
using System.Linq;
using Celeste.Game.Components.Talking;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Cutscenes.Pinball;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;

namespace Celeste.Mods.StrawberryJam.Entities.Arcades
{
    [CustomEntity("SJ2021/PinballMachine")]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.PinballMachine")]
    internal class PinballMachine : SceneEntityState
    {
        private struct Debris
        {
            public Vector2 Direction;
            public float Percent, Duration;
            public bool Enabled;
        }

        private readonly GameApplication engine;
        private OriginalTalkComponent talk;
        private Sprite sprite;
        private Debris[] debris = new Debris[50];

        private PackedColor debrisColorFrom = SourceHexColor.HexToColor("f442d4"),
            debrisColorTo = SourceHexColor.HexToColor("000000");

        private AtlasEntry debrisTexture;
        private float bufferAlpha, bufferTimer;
        public float DistortionFade = 1f;

        public PinballMachine(ISourceEngineContext context, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)context).EntitySourceDataRegistration, data.Position + offset)
        {
            engine = (GameApplication)context;
            debrisTexture = engine.GraphicsResources.GameAtlas["particles/blob"];
            Depth = 2000;
            Add(talk = new OriginalTalkComponent(engine, new IntRect(-24, -8, 48, 40), new Vector2(-.5f, -20f),
                Interact, null));
            talk.PlayerMustBeFacing = false;
            Add(sprite = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("pinballMachine"));
            sprite.CenterOrigin();
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new PinballMachine(level.EngineContext, data, offset);

        public void Activate() => Add(new SimulationCoroutine(engine, ActivateRoutine(), true));

        public IEnumerator ActivateRoutine()
        {
            var level = (OriginalLevelSceneRoot)Scene;
            float debrisStart = 0f;
            sprite.Play("consoleLights", false, false);
            engine.AudioSession.Play("event:/classic/sfx2");
            yield return .3f;
            engine.AudioSession.Play("event:/classic/sfx1");
            yield return .24f;
            engine.AudioSession.Play("event:/classic/sfx7");
            yield return 1f;
            engine.AudioSession.Play("event:/classic/pico8_boot");
            yield return 2.5f;
            Add(new DisplacementRenderHookState(RenderDisplacement));
            var light = level.Lighting;
            sprite.Play("glitchLoop", false, false);
            yield return .5f;
            sprite.Play("glitchEnd", false, false);
            while (true)
            {
                bufferAlpha = ScalarMath.Approach(bufferAlpha, 1f, engine.DeltaTime);
                bufferTimer += 4f * engine.DeltaTime;
                light.Alpha = ScalarMath.Approach(light.Alpha, .2f, engine.DeltaTime * .05f);
                if (debrisStart < debris.Length)
                {
                    int index = (int)debrisStart;
                    debris[index].Direction =
                        Vector2Geometry.AngleToVector(SourceRandomMath.NextFloat(engine.Random, (float)Math.PI * 2f),
                            1f);
                    debris[index].Enabled = true;
                    debris[index].Duration = .5f + SourceRandomMath.NextFloat(engine.Random, .7f);
                }

                debrisStart += engine.DeltaTime * 10f;
                for (int i = 0; i < debris.Length; i++)
                    if (debris[i].Enabled)
                    {
                        debris[i].Percent %= 1f;
                        debris[i].Percent += engine.DeltaTime / debris[i].Duration;
                    }

                yield return null;
            }
        }

        private void RenderDisplacement() => SourcePrimitiveDrawing.Rect(engine.CurrentContext, this, X - 28f, Y - 31f,
            54f, 30f, new PackedColor(.5f, .5f, .25f * DistortionFade * bufferAlpha, 1f));

        public void Interact(RoomPlayerSession player) =>
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new PinballIntroCutscene(engine, player, this));

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var level = (OriginalLevelSceneRoot)Scene;
            for (int i = 0; i < debris.Length; i++)
            {
                var d = debris[i];
                if (d.Enabled)
                {
                    switch (i % 3)
                    {
                        case 0: debrisColorFrom = SourceHexColor.HexToColor("ff5cf0"); break;
                        case 1: debrisColorFrom = SourceHexColor.HexToColor("82ffff"); break;
                        case 2: debrisColorFrom = SourceHexColor.HexToColor("ff4a4a"); break;
                    }

                    float ease = engine.EasingArithmetic.SineOut(d.Percent);
                    var position = Position + d.Direction * (1f - ease) * (190f - level.Zoom * 30f);
                    var color = PackedColor.Lerp(debrisColorFrom, debrisColorTo, ease);
                    float scale = ScalarMath.LerpClamp(1f, .2f, ease, engine.ScalarArithmetic);
                    SourceAtlasDrawing.DrawCentered(context, this, debrisTexture, position - new Vector2(0f, 15f),
                        color, scale, (float)i * .05f);
                }
            }
        }
    }
}
