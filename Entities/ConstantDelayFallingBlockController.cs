using Celeste.Core.Reflection.Runtime;
using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ConstantDelayFallingBlockController")]
    [CustomEntity("SJ2021/ConstantDelayFallingBlockController")]
    public sealed class ConstantDelayFallingBlockController : SceneEntityState
    {
        public ConstantDelayFallingBlockController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
        }

        public static IDisposable InstallHook()
        {
            return new Hook(
                typeof(OriginalFallingBlockState).GetMethod("PlayerWaitCheck",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Func<Func<OriginalFallingBlockState, bool>, OriginalFallingBlockState, bool>)PlayerWaitCheck);
        }

        private static bool PlayerWaitCheck(Func<OriginalFallingBlockState, bool> original,
            OriginalFallingBlockState block)
        {
            if (((ISourceScene)block.Scene).SourceScene.Entities
                .GetTrackedEntities(typeof(ConstantDelayFallingBlockController)).Count != 0)
                return false;
            return original(block);
        }
    }
}
