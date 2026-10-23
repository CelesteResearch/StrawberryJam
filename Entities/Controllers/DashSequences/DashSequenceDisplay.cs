// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Settings;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.Gui;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Configuration;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Controllers.DashSequences
{
    // This entity is added/removed from the scene automatically.
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.DashSequenceDisplay")]
    public class DashSequenceDisplay : SceneEntityState
    {
        private static readonly PackedColor correctA = new PackedColor(50, 205, 50, 255);
        private static readonly PackedColor correctB = new PackedColor(SourceColorParsing.HexToColor("1bd1bc")); // cyan

        private float drawlerp;
        private readonly AtlasEntry bg;
        private readonly ISourceEngineContext engine;
        private readonly Func<StrawberryJamSettings> settings;
        private readonly string controllerRegistryKey;
        private readonly Func<float, float> cubeOut, expoInOut;

        private readonly SortedList<int, AtlasEntry[]> dashCodes = new SortedList<int, AtlasEntry[]>();

        public int Index;
        private float lengthTarget, lengthPrev, lengthLerp;
        private AtlasEntry[] currentCodeArrows;
        private KeyValuePair<int, AtlasEntry[]>? nextCode;
        private float[] currentCodeArrowsAnim;
        private float animThreshold;
        private bool doNotRemove;

        private int codePosition;
        private readonly WigglerComponent wiggler;

        public DashSequenceDisplay(ISourceEngineContext engine, Func<StrawberryJamSettings> settings) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            this.settings = settings;
            controllerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog
                .Find(typeof(DashSequenceController)).Name;
            bg = ((ISourceGuiAtlasResources)engine).GuiAtlas["strawberryCountBG"];
            cubeOut = ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeOut;
            expoInOut =
                ((ISourceNamedEasingArithmetic)((ISourceEasingEnvironment)engine).EasingArithmetic).NamedEasers[
                    "ExpoInOut"];
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value |
                   ((IOriginalGameTagsEnvironment)engine).Tags.Global.Value |
                   ((IOriginalGameTagsEnvironment)engine).Tags.PauseUpdate.Value |
                   ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
            Depth = -100;
            Y = 96f;
            Add(wiggler = WigglerComponent.Create(((ISourceWigglerPoolScope)engine).WigglerPool, 0.5f, 2f));
            Add(new SimulationCoroutine(engine, ChangingDashCodeRoutine()));
        }

        public override void Update()
        {
            OriginalLevelSceneRoot level = ((OriginalLevelSceneRoot)Scene);

            float y = level.StrawberriesDisplay.DrawLerp > 0 ? 192 : 96;
            if (!level.TimerHidden)
            {
                if (((ISettingsContext)engine).Settings.SpeedrunClock == (int)SpeedrunType.Chapter)
                    y += 58f;
                else if (((ISettingsContext)engine).Settings.SpeedrunClock == (int)SpeedrunType.File)
                    y += 78f;
            }

            Y = ScalarMath.Approach(Y, y, engine.DeltaTime * 800f);

            bool show = currentCodeArrows != null && settings().DisplayDashSequence;
            drawlerp = ScalarMath.Approach(drawlerp, show ? 1 : 0, engine.DeltaTime * 2);
            lengthLerp = ScalarMath.Approach(lengthLerp, 1, engine.DeltaTime * 1.5f);

            if (currentCodeArrowsAnim != null)
                for (int i = 0; i < currentCodeArrowsAnim.Length; i++)
                    currentCodeArrowsAnim[i] = ScalarMath.Approach(currentCodeArrowsAnim[i],
                        i < animThreshold * currentCodeArrowsAnim.Length ? 1f : 0f, engine.DeltaTime * 2.5f);

            base.Update();
        }

        public void InitializeDashCodes(OriginalLevelSceneRoot level)
        {
            codePosition = 0;
            dashCodes.Clear();
            doNotRemove = true;
            DashSequenceController[] controllers = level.Scheduler.Entities
                .GetTrackedEntities(controllerRegistryKey).Cast<DashSequenceController>()
                .ToArray();
            if (controllers.Length != 0)
            {
                foreach (DashSequenceController controller in controllers)
                    dashCodes[controller.Index] = controller.DashCode.Select(v =>
                        ((ISourceGuiAtlasResources)engine).GuiAtlas[
                            $"controls/directions/{(int)v.X}x{(int)v.Y}"]).ToArray();

                if (dashCodes.Count != 0)
                    nextCode = dashCodes.First();
            }
            else
                nextCode = null;
        }

        public void ValidateInput()
        {
            codePosition++;
            wiggler.Start();
            if (codePosition >= currentCodeArrows.Length)
            {
                ((ISourceAudioEnvironment)engine).SourceAudio.Play("event:/game/01_forsaken_city/birdbros_thrust");
                KeyValuePair<int, AtlasEntry[]> next = dashCodes.FirstOrDefault(pair => pair.Key > Index);
                if (next.Value != null)
                    nextCode = next;
                else
                    nextCode = null;
            }
            else
                ((ISourceAudioEnvironment)engine).SourceAudio.Play("event:/game/06_reflection/supersecret_dashflavour");
        }

        public void Fail()
        {
            if (codePosition > 0)
                ((ISourceAudioEnvironment)engine).SourceAudio.Play("event:/strawberry_jam_2021/game/dash_seq/fail");
            codePosition = 0;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            InitializeDashCodes(((OriginalLevelSceneRoot)Scene));
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!(drawlerp <= 0f))
            {
                Vector2 pos = Vector2Interpolation.Lerp(new Vector2(-bg.FrameWidth, Y), Vector2.UnitY * Y,
                    cubeOut(drawlerp),
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                pos = new Vector2((float)Math.Round(pos.X), (float)Math.Round(pos.Y));

                float l = lengthPrev + (lengthTarget - lengthPrev) * cubeOut(lengthLerp);
                Vector2 bgPos = pos + Vector2.UnitX * (32 + (l - 7.5f) * 48);
                if (bgPos.X > 0f)
                    SourceAtlasDrawing.DrawJustified(context, this, bg, pos, new Vector2(0f, 0.5f), PackedColor.White,
                        new Vector2((bgPos.X - pos.X + 64) / bg.FrameWidth, 1.25f));
                SourceAtlasDrawing.DrawJustified(context, this, bg, bgPos, new Vector2(0f, 0.5f), PackedColor.White,
                    Vector2.One * 1.25f);

                if (currentCodeArrows != null)
                {
                    Vector2 arrowOffset = pos + Vector2.UnitX * 32;
                    for (int i = 0; i < currentCodeArrows.Length; i++)
                    {
                        PackedColor c = PackedColor.White;
                        float s = expoInOut(currentCodeArrowsAnim[i]) * 0.75f, r = 0;
                        if (i < codePosition)
                        {
                            c = ((ISettingsContext)engine).Settings.DisableFlashes
                                ? correctA
                                : PackedColor.Lerp(correctA, correctB,
                                    (((float)Math.Sin(((ISourceScene)Scene).SourceScene.Clock.RawTimeActive * 20f -
                                                      i / 2f) + 1f) / 2f));
                        }

                        if (i + 1 == codePosition)
                        {
                            s += wiggler.Value * 0.25f;
                            r = wiggler.Value * 0.25f;
                        }

                        SourceAtlasDrawing.DrawCentered(context, this, currentCodeArrows[i],
                            arrowOffset + Vector2.UnitX * (i * 48), c, new Vector2(s), r);
                    }
                }
            }
        }

        private IEnumerator ChangingDashCodeRoutine()
        {
            while (true)
            {
                doNotRemove = false;
                while (this.nextCode?.Value == currentCodeArrows)
                    yield return null;
                KeyValuePair<int, AtlasEntry[]> nextCode = this.nextCode ?? default;

                int count = -1;
                if (nextCode.Value != null)
                {
                    count = nextCode.Value.Length;
                    // don't swap code when the arrows are the same (but the references to the arrays aren't)
                    if (currentCodeArrows != null && nextCode.Value.SequenceEqual(currentCodeArrows))
                    {
                        currentCodeArrows = nextCode.Value;
                        continue;
                    }
                }
                else if (currentCodeArrows != null)
                    yield return 0.5f;

                bool changedLength = false;
                if (currentCodeArrows != null)
                {
                    while (animThreshold > 0f)
                    {
                        animThreshold = ScalarMath.Approach(animThreshold, 0f, engine.DeltaTime * 4f);
                        yield return null;
                    }

                    lengthPrev = lengthTarget;
                    lengthLerp = 0f;
                    lengthTarget = count;
                    changedLength = true;
                    yield return nextCode.Value == null ? 1f : 0.3f;
                }

                currentCodeArrows = null;
                if (nextCode.Value != null)
                {
                    if (!changedLength)
                    {
                        lengthPrev = lengthTarget;
                        lengthLerp = 0f;
                        lengthTarget = count;
                    }

                    Index = nextCode.Key;
                    currentCodeArrows = nextCode.Value;
                    currentCodeArrowsAnim = new float[count];
                    codePosition = 0;

                    animThreshold = 0f;
                    while (animThreshold < 1f)
                    {
                        animThreshold = ScalarMath.Approach(animThreshold, 1f, engine.DeltaTime * 4f);
                        yield return null;
                    }
                }
                else if (!doNotRemove)
                {
                    RemoveSelf();
                }
            }
        }
    }
}
