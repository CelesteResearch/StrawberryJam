using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Interaction.Boosters;
using Celeste.Mods.StrawberryJam.Entities.Boosters;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Boosters
{
    public sealed class DirectionalBoosterProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, DirectionalBoosterProcess> Owners = new();
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly List<IDisposable> hooks = new(3);
        public ISourceEngineContext Engine { get; }
        public StrawberryJamModule Module { get; }

        public DirectionalBoosterProcess(ISourceEngineContext engine, StrawberryJamModule module)
        {
            Engine = engine;
            Module = module;
            Owners.Add(engine, this);
            try
            {
                var boosted = typeof(OriginalBoosterState).GetMethod(nameof(OriginalBoosterState.PlayerBoosted), Flags);
                hooks.Add(new Hook(boosted,
                    (Action<Action<OriginalBoosterState, RoomPlayerSession, Vector2>, OriginalBoosterState,
                        RoomPlayerSession, Vector2>)PlayerBoosted));
                hooks.Add(new ILHook(boosted, PatchSpinAnimation));
                hooks.Add(new ILHook(typeof(OriginalBoosterState).GetMethod("AppearParticles", Flags),
                    PatchAppearParticles));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public static DirectionalBoosterProcess For(ISourceEngineContext engine)
            => Owners.TryGetValue(engine, out var owner)
                ? owner
                : throw new InvalidOperationException("Directional booster hooks are not loaded in this engine.");

        private bool Own(OriginalBoosterState booster)
            => booster is DirectionalBooster directional && ReferenceEquals(directional.Engine, Engine);

        private void PlayerBoosted(Action<OriginalBoosterState, RoomPlayerSession, Vector2> orig,
            OriginalBoosterState booster, RoomPlayerSession player, Vector2 direction)
        {
            orig(booster, player, direction);
            if (Own(booster)) ((DirectionalBooster)booster).DirectionalSprite.FlipX = direction.X < .1f;
        }

        private void PatchSpinAnimation(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdstr("spin"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<string, OriginalBoosterState, Vector2, string>>((animation, booster, direction)
                => Own(booster) ? AnimationForDirection(direction) : animation);
        }

        private void PatchAppearParticles(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                instruction => instruction.MatchCall<OriginalBoosterState>("get_P_RedAppear"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<ParticleTypePresentation, OriginalBoosterState, ParticleTypePresentation>>((
                    particle, booster)
                => Own(booster) ? ((DirectionalBooster)booster).AppearParticleType : particle);
        }

        public static string AnimationForDirection(Vector2 direction)
        {
            int x = Math.Abs(direction.X) < .1 ? 0 : Math.Sign(direction.X);
            int y = Math.Abs(direction.Y) < .1 ? 0 : Math.Sign(direction.Y);
            if (x == 0)
            {
                if (y == -1) return "spin_up";
                if (y == 1) return "spin_down";
                return "";
            }

            if (y == -1) return "spin_upright";
            if (y == 1) return "spin_downright";
            return "spin_right";
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            if (Owners.TryGetValue(Engine, out var current) && ReferenceEquals(current, this)) Owners.Remove(Engine);
        }
    }
}
