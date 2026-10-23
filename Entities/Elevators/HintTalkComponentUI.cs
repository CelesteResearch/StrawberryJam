using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Audio;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Components.Talking;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Localization.Lookup;

namespace Celeste.Mods.StrawberryJam.Entities.Elevators
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.HintTalkComponentUI")]
    public class HintTalkComponentUI : OriginalTalkUiEntity
    {
        private readonly SolarElevator elevator;
        private readonly string text;
        private float lerp, prev, textTimer;

        public HintTalkComponentUI(OriginalTalkComponent handler, SolarElevator elevator) : base(handler)
        {
            this.elevator = elevator;
            text = ((ISourceDialogEnvironment)elevator.Engine).Dialog.Clean(elevator.HoldableHintDialog);
        }

        public override void Update()
        {
            base.Update();
            bool show = Highlighted && !elevator.IsCarryingHoldable && !elevator.Moving;
            textTimer = show ? ScalarMath.Approach(textTimer, 0, elevator.Engine.DeltaTime) : .25f;
            prev = lerp;
            lerp = ScalarMath.Approach(lerp, show && textTimer <= 0 ? 1 : 0, elevator.Engine.DeltaTime * 6);
            ISourceAudioEventReference sound = null;
            if (prev == 1 && lerp < 1)
                sound = ((ISourceSpatialAudio)elevator.Engine).Play("event:/ui/game/textbox_other_out",
                    elevator.Position);
            else if (prev == 0 && lerp > 0)
                sound = ((ISourceSpatialAudio)elevator.Engine).Play("event:/ui/game/textbox_other_in",
                    elevator.Position);
            if (sound != null) sound.SetPitch(1.75f);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var level = (OriginalLevelSceneRoot)Scene;
            if (level.FrozenOrPaused || !(slide > 0) || Handler.Entity == null) return;
            Vector2 at = ((ISourceEntityPosition)Handler.Entity).Position + Handler.DrawAt -
                         Vector2Geometry.Floor(level.Camera.Position);
            if (Process.CurrentSave != null && Process.CurrentSave.Assists.MirrorMode) at.X = 320 - at.X;
            at *= 6;
            at.Y += (float)Math.Sin(timer * 4) * 12 + 64 * (1 - Process.EasingArithmetic.CubeOut(slide)) + 12;
            float opacity = Process.EasingArithmetic.CubeInOut(slide) * alpha * lerp;
            float trigger = textTimer > 0 ? 1 : 0;
            float scale = Math.Max(wiggler.Value * trigger * lerp * .1f + Process.EasingArithmetic.CubeOut(lerp) * .65f,
                0);
            Process.ActiveFont.DrawOutline(context, this, text, at, Vector2.One * .5f, Vector2.One * scale,
                PackedColor.White * opacity, 2, PackedColor.Black);
        }
    }
}
