using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Audio;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Mechanisms.DashSwitches;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.StrawberryJam.Entities.Switches;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.Particles;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Switches
{
    public sealed class ResizableDashSwitchRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ResizableDashSwitchRuntime> owners = new();
        private readonly List<IDisposable> hooks = new();
        public ISourceEngineContext Engine { get; }
        public StrawberryJamModule Module { get; }
        internal ParticleTypePresentation P_Fizzle;
        internal ParticleTypePresentation P_Shatter;

        public static ResizableDashSwitchRuntime For(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var runtime)
                ? runtime
                : throw new InvalidOperationException("Resizable dash switch module is not loaded for this engine.");

        public ResizableDashSwitchRuntime(ISourceEngineContext engine, StrawberryJamModule module)
        {
            Engine = engine;
            Module = module;
            owners.Add(engine, this);
            try
            {
                var dashed = typeof(OriginalDashSwitchState).GetMethod(nameof(OriginalDashSwitchState.OnDashed));
                hooks.Add(new Hook(dashed,
                    (Func<Func<OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult>,
                        OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult>)OnDashed));
                hooks.Add(new ILHook(dashed, ModifyPressedEffects));
                hooks.Add(new ILHook(typeof(OriginalDashSwitchState).GetMethod(nameof(OriginalDashSwitchState.Update)),
                    ModifyIdleSounds));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(OriginalDashSwitchState state) =>
            state is ResizableDashSwitch entity && ReferenceEquals(entity.EngineContext, Engine);

        private DashCollisionResult OnDashed(
            Func<OriginalDashSwitchState, RoomPlayerSession, Vector2, DashCollisionResult> orig,
            OriginalDashSwitchState self, RoomPlayerSession player, Vector2 direction)
        {
            if (self is ResizableDashSwitch entity && ReferenceEquals(entity.EngineContext, Engine) &&
                entity.CanPress(direction))
                entity.OnPressed(player, direction);
            return orig(self, player, direction);
        }

        private ISourceAudioEventReference ModifyVolume(ISourceAudioEventReference sound, OriginalDashSwitchState self,
            float volume)
        {
            if (Owns(self)) sound.SetVolume(volume);
            return sound;
        }

        private void ModifyPressedEffects(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.Operand is MethodReference method && method.Name == "set_Position");
            var after = cursor.Clone();
            after.GotoNext(MoveType.After,
                i => i.MatchCallvirt<ParticleSystemPresentation>(nameof(ParticleSystemPresentation.Emit)));
            after.GotoNext(MoveType.After,
                i => i.MatchCallvirt<ParticleSystemPresentation>(nameof(ParticleSystemPresentation.Emit)));
            var target = after.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<OriginalDashSwitchState, bool>>(Owns);
            cursor.Emit(OpCodes.Brtrue, target);
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After, IsPlay);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldc_R4, .8f);
            cursor
                .EmitDelegate<
                    Func<ISourceAudioEventReference, OriginalDashSwitchState, float, ISourceAudioEventReference>>(
                    ModifyVolume);
        }

        private static bool IsPlay(Instruction instruction) => instruction.Operand is MethodReference method &&
                                                               method.Name == nameof(ISourceSpatialAudio.Play) &&
                                                               method.DeclaringType.FullName ==
                                                               typeof(ISourceSpatialAudio).FullName;

        private void ModifyIdleSounds(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, IsPlay))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldc_R4, 0f);
                cursor
                    .EmitDelegate<
                        Func<ISourceAudioEventReference, OriginalDashSwitchState, float, ISourceAudioEventReference>>(
                        ModifyVolume);
                count++;
            }

            if (count != 2)
                throw new InvalidOperationException(
                    "DashSwitch.Update depress/return audio sites differ from the source body.");
        }

        public void LoadParticles()
        {
            P_Fizzle = new ParticleTypePresentation
            {
                Color = SourceHexColor.HexToColor("dce34f"),
                Color2 = SourceHexColor.HexToColor("fbffaf"),
                ColorMode = ParticleTypePresentation.ColorModes.Blink,
                Size = 1f,
                SizeRange = 0f,
                SpeedMin = 20f,
                SpeedMax = 70f,
                LifeMin = 0.5f,
                LifeMax = 0.9f,
                Direction = ((float)Math.PI / 2f),
                DirectionRange = ((float)Math.PI / 4f) / 1.5f,
                SpeedMultiplier = 0.2f
            };
            P_Shatter = new ParticleTypePresentation
            {
                Source = ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas.Entries["particles/triangle"],
                Color = SourceHexColor.HexToColor("fff089"),
                Color2 = SourceHexColor.HexToColor("fff089"),
                FadeMode = ParticleTypePresentation.FadeModes.Late,
                LifeMin = 0.35f,
                LifeMax = 0.5f,
                Size = 1f,
                Direction = (float)Math.PI * 1.5f,
                DirectionRange = ((float)Math.PI / 2f),
                SpeedMin = 120f,
                SpeedMax = 140f,
                SpeedMultiplier = 0.01f,
                RotationMode = ParticleTypePresentation.RotationModes.Random,
                SpinMin = -((float)Math.PI / 2f),
                SpinMax = 4.712389f,
                SpinFlippedChance = true
            };
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            if (owners.TryGetValue(Engine, out var owner) && ReferenceEquals(owner, this)) owners.Remove(Engine);
            P_Fizzle = null;
            P_Shatter = null;
        }
    }
}
