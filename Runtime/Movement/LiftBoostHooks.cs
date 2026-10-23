using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Mods.StrawberryJam.Triggers.Movement;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Movement
{
    public sealed class LiftBoostHooks : IDisposable
    {
        private static readonly Action<PixelActor, Vector2> SetLastLiftSpeed = CreateSetter<Vector2>("lastLiftSpeed");

        private static readonly Action<PixelActor, Vector2> SetCurrentLiftSpeed =
            CreateSetter<Vector2>("currentLiftSpeed");

        private static readonly Action<PixelActor, float> SetLiftSpeedTimer = CreateSetter<float>("liftSpeedTimer");
        private readonly ISourceEngineContext engine;
        private readonly string registryKey;
        private readonly Hook hook;

        public LiftBoostHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            registryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(LiftBoostTrigger))
                .RegistryKey;
            hook = new Hook(typeof(PixelActor).GetProperty(nameof(PixelActor.LiftSpeed)).SetMethod,
                (Action<Action<PixelActor, Vector2>, PixelActor, Vector2>)SetLiftSpeed);
        }

        private static Action<PixelActor, T> CreateSetter<T>(string name)
        {
            var actor = Expression.Parameter(typeof(PixelActor), "actor");
            var value = Expression.Parameter(typeof(T), "value");
            var field = typeof(PixelActor).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return Expression
                .Lambda<Action<PixelActor, T>>(Expression.Assign(Expression.Field(actor, field), value), actor, value)
                .Compile();
        }

        private void SetLiftSpeed(Action<PixelActor, Vector2> orig, PixelActor self, Vector2 value)
        {
            var actor = self.SourceColliderOwner as SceneEntityState;
            if (actor?.Scene is not ISourceEngineContextScene scene || !ReferenceEquals(scene.EngineContext, engine))
            {
                orig(self, value);
                return;
            }

            _ = self.LiftSpeed;
            var trigger = actor.CollideFirst<LiftBoostTrigger>(registryKey);
            if (trigger != null)
            {
                Vector2 lastLiftSpeed = self.LastLiftSpeed;
                Vector2 currentLiftSpeed = value;
                if (value != Vector2.Zero && self.LiftSpeedGraceTime > 0f)
                {
                    if (value.X == 0f && lastLiftSpeed.X != 0f) currentLiftSpeed.X = lastLiftSpeed.X;
                    if (value.Y == 0f && lastLiftSpeed.Y != 0f) currentLiftSpeed.Y = lastLiftSpeed.Y;
                    SetLastLiftSpeed(self, currentLiftSpeed);
                    SetCurrentLiftSpeed(self, currentLiftSpeed);
                    SetLiftSpeedTimer(self, self.LiftSpeedGraceTime);
                }
            }
            else orig(self, value);
        }

        public void Dispose() => hook.Dispose();
    }
}
