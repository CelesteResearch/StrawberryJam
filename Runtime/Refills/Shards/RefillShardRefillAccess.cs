using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Components;
using Celeste.Game.Gameplay.Collectibles.Refills;

namespace Celeste.Mods.StrawberryJam.Runtime.Refills.Shards
{
    internal static class RefillShardRefillAccess
    {
        private static readonly Action<OriginalRefillState, float> Timer = BindTimer();
        private static readonly Action<OriginalRefillState> Return = BindRespawn();
        private static readonly FieldInfo Sprite = Field("sprite");
        private static readonly FieldInfo Flash = Field("flash");
        private static readonly FieldInfo Outline = Field("outline");

        private static FieldInfo Field(string name) => typeof(OriginalRefillState).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(name);

        internal static void HideComponents(OriginalRefillState refill)
        {
            // Only called at Added, retaining the original chained assignment order.
            ((SourceComponentState)Outline.GetValue(refill)).Visible = false;
            ((SourceComponentState)Flash.GetValue(refill)).Visible = false;
            ((SourceComponentState)Sprite.GetValue(refill)).Visible = false;
        }

        internal static void SetRespawnTimer(OriginalRefillState refill, float time) => Timer(refill, time);
        internal static void Respawn(OriginalRefillState refill) => Return(refill);

        private static Action<OriginalRefillState, float> BindTimer()
        {
            var method = new DynamicMethod("SetShardRefillRespawnTimer", typeof(void),
                new[] { typeof(OriginalRefillState), typeof(float) }, typeof(RefillShardRefillAccess).Module, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, Field("respawnTimer"));
            il.Emit(OpCodes.Ret);
            return (Action<OriginalRefillState, float>)method.CreateDelegate(
                typeof(Action<OriginalRefillState, float>));
        }

        private static Action<OriginalRefillState> BindRespawn()
        {
            var target =
                typeof(OriginalRefillState).GetMethod("Respawn", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException("Refill.Respawn");
            return (Action<OriginalRefillState>)target.CreateDelegate(typeof(Action<OriginalRefillState>));
        }
    }
}
