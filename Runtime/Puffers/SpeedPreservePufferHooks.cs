using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.StrawberryJam.Entities.Hazards;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Runtime;
using Celeste.Simulation.Gameplay.Player.Audio.Frame;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Puffers
{
    public sealed class SpeedPreservePufferHooks : IDisposable
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private sealed class DataComponent : SourceComponentState
        {
            public float storedSpeed;
            public bool speedPreservingPuffer;

            public DataComponent() : base(false, false)
            {
            }
        }

        private readonly ISourceEngineContext engine;
        private ILHook update, constructor, explode;

        public SpeedPreservePufferHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                update = new ILHook(typeof(PlayerFrameMotion).GetMethod(nameof(PlayerFrameMotion.Update)),
                    ModifyUpdate);
                constructor = new ILHook(
                    typeof(OriginalPufferState).GetConstructor(new[]
                    {
                        typeof(Celeste.Physics.Collision.PixelActor), typeof(Vector2), typeof(bool),
                        typeof(ISourceEngineContext),
                        typeof(Celeste.Simulation.Gameplay.Platforms.Foundation.IPlatformTargetMotionPolicy)
                    }), ModifyConstructor);
                explode = new ILHook(
                    typeof(OriginalPufferState).GetMethod("Explode", BindingFlags.NonPublic | BindingFlags.Instance),
                    ModifyExplode);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private RoomPlayerSession Player(object scene) => scene is ISourceScene source
            ? source.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(sourcePlayerRegistryKey)
            : null;

        private void ModifyUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(i => i.MatchStfld<Vector2>(nameof(Vector2.X)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld,
                typeof(PlayerFrameMotion).GetField("world", BindingFlags.NonPublic | BindingFlags.Instance));
            cursor.EmitDelegate<Func<float, IPlayerFrameWorld, float>>(RestoreBoost);
        }

        private float RestoreBoost(float original, IPlayerFrameWorld world)
        {
            if (!(world is RoomPlayerWorld sourceWorld)) return original;
            var player = sourceWorld.SourcePlayer;
            if (!ReferenceEquals(player.EngineContext, engine)) return original;
            var data = player.Get<DataComponent>();
            return data != null && data.speedPreservingPuffer
                ? player.Data.Motion.Normal.Speed.X +
                  Math.Abs(.2f * original) * Math.Sign(player.Data.Motion.Normal.Speed.X)
                : original;
        }

        private void ModifyConstructor(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.MatchCallOrCallvirt<SineWaveState>(nameof(SineWaveState.Randomize))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg, 4);
                cursor.EmitDelegate<Action<OriginalPufferState, ISourceEngineContext>>((self, owner) =>
                {
                    if (ReferenceEquals(owner, engine) && self is SpeedPreservePuffer puffer && puffer.Static)
                        puffer.ResetIdleSine();
                });
            }
        }

        private void ModifyExplode(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.Operand is MethodReference method &&
                                                 method.DeclaringType.FullName ==
                                                 "Celeste.Scene.Entities.SourceSceneCollision" &&
                                                 method.Name == "FirstLine");
            cursor.GotoNext(MoveType.After, i => i.OpCode == OpCodes.Brtrue || i.OpCode == OpCodes.Brtrue_S);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalPufferState>>(SaveSpeed);
            cursor.GotoNext(MoveType.After,
                i => i.MatchCallOrCallvirt<RoomPlayerSession>(nameof(RoomPlayerSession.ExplodeLaunch)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<OriginalPufferState>>(RestoreSpeed);
        }

        private bool Owns(OriginalPufferState puffer) => puffer.Scene is ISourceEngineContextScene scene &&
                                                         ReferenceEquals(scene.EngineContext, engine);

        private void SaveSpeed(OriginalPufferState puffer)
        {
            if (!Owns(puffer)) return;
            var player = Player(engine.Frame.Scenes.CurrentScene);
            if (player == null) return;
            var data = player.Get<DataComponent>();
            if (data == null) player.Add(data = new DataComponent());
            data.storedSpeed = player.Data.Motion.Normal.Speed.X;
        }

        private void RestoreSpeed(OriginalPufferState puffer)
        {
            if (!Owns(puffer)) return;
            var player = Player(puffer.Scene);
            if (player == null) return;
            var data = player.Get<DataComponent>();
            if (data == null) return;
            if (puffer is SpeedPreservePuffer)
            {
                player.Data.Motion.Normal.Speed.X +=
                    Math.Abs(data.storedSpeed) * Math.Sign(player.Data.Motion.Normal.Speed.X);
                data.speedPreservingPuffer = true;
            }
            else data.speedPreservingPuffer = false;
        }

        public void Dispose()
        {
            update?.Dispose();
            update = null;
            constructor?.Dispose();
            constructor = null;
            explode?.Dispose();
            explode = null;
        }
    }
}
