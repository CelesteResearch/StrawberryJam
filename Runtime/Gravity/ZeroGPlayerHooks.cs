using System;
using Celeste.Mods.StrawberryJam.Runtime.Gravity;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using System.Numerics;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Gravity
{
    public sealed class ZeroGPlayerHooks : IDisposable
    {
        private readonly ZeroGRuntime runtime;
        private Hook update, climbHop;

        public ZeroGPlayerHooks(ZeroGRuntime runtime)
        {
            this.runtime = runtime;
            try
            {
                update = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)Update);
                climbHop = new Hook(typeof(ClimbSequence).GetMethod(nameof(ClimbSequence.Hop)),
                    (Action<Action<ClimbSequence>, ClimbSequence>)ClimbHop);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ClimbHop(Action<ClimbSequence> orig, ClimbSequence self)
        {
            if (ReferenceEquals(self.EngineContext, runtime.Engine) && runtime.Session.ZeroG)
            {
                int i = 0;
                while (self.World.FirstSolidAt(Vector2.UnitX * self.Data.Facing - Vector2.UnitY * i) != null) i++;
                self.World.NudgePosition(-Vector2.UnitY * i);
            }

            orig(self);
        }

        private void Update(Action<RoomPlayerSession> orig, RoomPlayerSession self)
        {
            if (!ReferenceEquals(self.EngineContext, runtime.Engine))
            {
                orig(self);
                return;
            }

            if (self.JustRespawned) runtime.SoftlockFrames = 0;
            orig(self);
            if (!self.JustRespawned && runtime.Session.ZeroG)
            {
                if (self.Position == self.Data.PreviousPosition && self.Data.Dashes == 0 &&
                    (self.Data.Stamina < 20 || !self.World.Contacts.CanUnDuck ||
                     (!self.World.ClimbCheck(-1) && !self.World.ClimbCheck(1))))
                    runtime.SoftlockFrames++;
                else runtime.SoftlockFrames = 0;
                if (runtime.SoftlockFrames > 240)
                {
                    runtime.SoftlockFrames = 0;
                    self.Die(Vector2.Zero, true, false);
                }
            }
        }

        public void Dispose()
        {
            climbHop?.Dispose();
            update?.Dispose();
            climbHop = update = null;
        }
    }
}
