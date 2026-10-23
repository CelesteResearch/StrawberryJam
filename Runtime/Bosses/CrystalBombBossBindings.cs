using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Enemies.BadelineBoss.Construction;
using Celeste.Game.Gameplay.Enemies.BadelineBoss.Progression;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.CavernHelper.Entities.Explosives;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.StrawberryJam.Runtime.Bosses
{
    internal static class CrystalBombBossBindings
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, Func<IOriginalFinalBossFactory, FinalBossProgressionPolicy>>
            progressions = new();

        internal static readonly Action<CrystalBomb> Explode = (Action<CrystalBomb>)typeof(CrystalBomb)
            .GetMethod("Explode", Flags).CreateDelegate(typeof(Action<CrystalBomb>));

        internal static readonly Func<CrystalBomb, bool> Exploded = Read<CrystalBomb, bool>("exploded");

        internal static readonly Func<CrystalBomb, CircleColliderGeometry> BombRadius =
            Read<CrystalBomb, CircleColliderGeometry>("pushRadius");

        internal static readonly Func<OriginalSeekerState, CircleColliderGeometry> SeekerRadius =
            Read<OriginalSeekerState, CircleColliderGeometry>("pushRadius");

        internal static readonly Func<OriginalPufferState, CircleColliderGeometry> PufferRadius =
            Read<OriginalPufferState, CircleColliderGeometry>("pushRadius");

        internal static FinalBossProgressionPolicy Progression(IOriginalFinalBossFactory factory)
        {
            Type type = factory.GetType();
            if (!progressions.TryGetValue(type, out var getter))
            {
                FieldInfo field = type.GetField("progression", Flags);
                var method = new DynamicMethod("ReadBossProgression", typeof(FinalBossProgressionPolicy),
                    new[] { typeof(IOriginalFinalBossFactory) }, typeof(CrystalBombBossBindings), true);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, type);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
                getter = (Func<IOriginalFinalBossFactory, FinalBossProgressionPolicy>)method.CreateDelegate(
                    typeof(Func<IOriginalFinalBossFactory, FinalBossProgressionPolicy>));
                progressions.Add(type, getter);
            }

            return getter(factory);
        }

        private static Func<T, V> Read<T, V>(string name)
        {
            var method = new DynamicMethod("ReadBossExplosion" + name, typeof(V), new[] { typeof(T) },
                typeof(CrystalBombBossBindings), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(T).GetField(name, Flags));
            il.Emit(OpCodes.Ret);
            return (Func<T, V>)method.CreateDelegate(typeof(Func<T, V>));
        }
    }
}
