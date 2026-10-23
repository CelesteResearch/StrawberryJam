using Celeste.Core.Display;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Graphics.Display;
using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Controls.Source;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Camera;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Presentation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Effects.Trails.Lifecycle;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Profiles;
using Celeste.Game.Entities.BadelineDummy;
using Celeste.Game.Stories.Farewell.FinalLaunch;

namespace Celeste.Mods.StrawberryJam.Entities.Boosters
{
    public partial class CustomBadelineBoost
    {
        public override void Update()
        {
            if (sprite.Visible && CurrentLevel.Clock.OnInterval(0.05f, engine))
            {
                CurrentLevel.ParticlesBG.Emit(Process.ParticleTypes.BadelineBoost.P_Ambience, 1, base.Center,
                    Vector2.One * 3f);
            }

            if (holding != null)
            {
                holding.Speed = Vector2.Zero;
            }

            if (!travelling)
            {
                RoomPlayerSession entity = FirstPlayer();
                if (entity != null)
                {
                    float num = ScalarMath.ClampedMap(VectorArithmetic.Length(entity.Center - Position), 16f, 64f, 12f,
                        0f);
                    Vector2 vector = VectorArithmetic.SafeNormalize(entity.Center - Position);
                    sprite.Position = VectorArithmetic.Approach(sprite.Position, vector * num, 32f * engine.DeltaTime);
                }
            }

            light.Visible = (bloom.Visible = sprite.Visible || stretch.Visible);
            base.Update();
        }

        private void Finish()
        {
            CurrentLevel.Displacement.AddBurst(base.Center, 0.5f, 24f, 96f, 0.4f);
            CurrentLevel.Particles.Emit(Process.ParticleTypes.BadelineOldsite.P_Vanish, 12, base.Center,
                Vector2.One * 6f);
            CurrentLevel.CameraLockMode = RoomCameraLockMode.None;
            CurrentLevel.CameraOffset = new Vector2(0f, -16f);
            RemoveSelf();
        }
    }
}
