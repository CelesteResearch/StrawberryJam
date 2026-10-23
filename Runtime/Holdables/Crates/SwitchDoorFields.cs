using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Graphics.Animation;
using Celeste.Mods.Batteries.Entities.Gates;

namespace Celeste.Mods.StrawberryJam.Runtime.Holdables.Crates
{
    internal static class SwitchDoorFields
    {
        internal static readonly Func<BatteryGate, Sprite> ReadSprite = Getter<Sprite>("sprite");
        internal static readonly Func<BatteryGate, bool> ReadVertical = Getter<bool>("vertical");
        internal static readonly Action<BatteryGate, Sprite> WriteSprite = Setter<Sprite>("sprite");

        private static Func<BatteryGate, T> Getter<T>(string name)
        {
            var field = typeof(BatteryGate).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var method = new DynamicMethod("ReadBatteryGate" + name, typeof(T), new[] { typeof(BatteryGate) },
                typeof(SwitchDoorFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<BatteryGate, T>)method.CreateDelegate(typeof(Func<BatteryGate, T>));
        }

        private static Action<BatteryGate, T> Setter<T>(string name)
        {
            var field = typeof(BatteryGate).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            var method = new DynamicMethod("WriteBatteryGate" + name, typeof(void),
                new[] { typeof(BatteryGate), typeof(T) }, typeof(SwitchDoorFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<BatteryGate, T>)method.CreateDelegate(typeof(Action<BatteryGate, T>));
        }
    }
}
