using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using Celeste.Core.Scheduling;
using Celeste.Core.Display;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Mods.StrawberryJam.Entities.Holdables.Lanterns;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Lanterns
{
    public sealed partial class SkyLanternRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SkyLanternRuntime> owners = new();
        private static readonly Func<PlayerHoldingController, RoomPlayerSession> Player = BindPlayer();
        public ISourceEngineContext Engine { get; }
        public StrawberryJamModule Module { get; }

        public ParticleTypePresentation
            ParticleGlideDown, ParticleGlide, ParticlePlatform, ParticleGlow, ParticleExpand;

        private Hook throwHook;
        private SkyLanternFallSpeedHooks fallSpeedHooks;
        private Hook pickupHook;

        public static SkyLanternRuntime For(ISourceEngineContext engine) => owners.TryGetValue(engine, out var runtime)
            ? runtime
            : throw new InvalidOperationException("SkyLantern module is not loaded for this engine.");

        public SkyLanternRuntime(ISourceEngineContext engine, StrawberryJamModule module)
        {
            Engine = engine;
            Module = module;
            owners.Add(engine, this);
            try
            {
                pickupHook = new Hook(typeof(PlayerHoldingController).GetMethod("PickupCoroutine",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<PlayerHoldingController, System.Collections.IEnumerator>, PlayerHoldingController,
                        System.Collections.IEnumerator>)OnPickupCoroutine);
                fallSpeedHooks = new SkyLanternFallSpeedHooks(engine);
                throwHook = new Hook(typeof(PlayerHoldingController).GetMethod(nameof(PlayerHoldingController.Throw)),
                    (Action<Action<PlayerHoldingController>, PlayerHoldingController>)OnThrow);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static Func<PlayerHoldingController, RoomPlayerSession> BindPlayer()
        {
            var self = Expression.Parameter(typeof(PlayerHoldingController), "self");
            return Expression.Lambda<Func<PlayerHoldingController, RoomPlayerSession>>(
                Expression.Field(self,
                    typeof(PlayerHoldingController).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic)),
                self).Compile();
        }

        private void OnThrow(Action<PlayerHoldingController> original, PlayerHoldingController self)
        {
            if (self.Holding?.Entity is SkyLantern lantern && ReferenceEquals(lantern.EngineContext, Engine))
            {
                var player = Player(self);
                if (self.Input.MoveY.Value == 1 && self.Input.MoveX.Value != 0)
                {
                    ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)Engine).RumbleSession.Rumble(2, 0);
                    self.Holding.Release(Vector2.UnitX * self.Data.Facing);
                    player.Data.Motion.Normal.Speed.X += 80f * -self.Data.Facing;
                    player.Play("event:/char/madeline/crystaltheo_throw");
                    player.Sprite.Play("throw", false, false);
                    self.SetHolding(null);
                    return;
                }

                if (self.Input.MoveY.Value == -1)
                {
                    ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)Engine).RumbleSession.Rumble(2, 0);
                    self.Holding.Release(Vector2.UnitX * self.Data.Facing);
                    player.Play("event:/char/madeline/crystaltheo_throw");
                    player.Sprite.Play("throw", false, false);
                    self.SetHolding(null);
                    return;
                }
            }

            original(self);
        }

        public void LoadParticles()
        {
            if (ParticleGlideDown == null)
                ParticleGlideDown = new ParticleTypePresentation
                {
                    Acceleration = Vector2.UnitY * 60,
                    SpeedMin = 30f,
                    SpeedMax = 40f,
                    Direction = -1.5707964f,
                    DirectionRange = 1.5707964f,
                    LifeMin = 0.6f,
                    LifeMax = 1.2f,
                    ColorMode = ParticleTypePresentation.ColorModes.Blink,
                    FadeMode = ParticleTypePresentation.FadeModes.Late,
                    Color = Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor("951a00"),
                    Color2 = Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor("b94f00"),
                    Source =
                        ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas[
                            "particles/rect"],
                    Size = 0.5f,
                    SizeRange = 0.2f,
                    RotationMode = ParticleTypePresentation.RotationModes.Random
                };
            if (ParticleGlide == null)
                ParticleGlide = new ParticleTypePresentation(ParticleGlideDown)
                {
                    Acceleration = Vector2.UnitY * -10f,
                    SpeedMin = 50f,
                    SpeedMax = 60f
                };
            if (ParticlePlatform == null)
                ParticlePlatform = new ParticleTypePresentation
                {
                    Acceleration = Vector2.UnitY * -60f,
                    SpeedMin = 5f,
                    SpeedMax = 20f,
                    Direction = 1 / 2 * (float)Math.PI,
                    LifeMin = 0.6f,
                    LifeMax = 1.4f,
                    FadeMode = ParticleTypePresentation.FadeModes.Late,
                    Size = 1f
                };
            if (ParticleGlow == null)
                ParticleGlow = new ParticleTypePresentation
                {
                    SpeedMin = 8f,
                    SpeedMax = 16f,
                    DirectionRange = (float)Math.PI * 2,
                    LifeMin = 0.4f,
                    LifeMax = 0.8f,
                    Size = 1f,
                    FadeMode = ParticleTypePresentation.FadeModes.Late,
                    Color = Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor("daa600"),
                    Color2 = Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor("da8200"),
                    ColorMode = ParticleTypePresentation.ColorModes.Blink
                };
            if (ParticleExpand == null)
                ParticleExpand = new ParticleTypePresentation(ParticleGlow)
                {
                    SpeedMin = 40f,
                    SpeedMax = 80f,
                    SpeedMultiplier = 0.2f,
                    LifeMin = 0.6f,
                    LifeMax = 1.2f,
                    DirectionRange = 3 / 4 * (float)Math.PI
                };
        }

        public void Dispose()
        {
            pickupHook?.Dispose();
            pickupHook = null;
            fallSpeedHooks?.Dispose();
            fallSpeedHooks = null;
            throwHook?.Dispose();
            throwHook = null;
            if (owners.TryGetValue(Engine, out var runtime) && ReferenceEquals(runtime, this)) owners.Remove(Engine);
            ParticleGlideDown = ParticleGlide = ParticlePlatform = ParticleGlow = ParticleExpand = null;
        }
    }
}
