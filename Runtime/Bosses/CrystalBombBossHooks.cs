using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.BadelineBoss;
using Celeste.Game.Gameplay.Enemies.BadelineBoss.Progression;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Graphics.Animation;
using Celeste.Mods.CavernHelper.Entities.Explosives;
using Celeste.Mods.StrawberryJam.Entities.Bosses;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.StrawberryJam.Runtime.Bosses
{
    public sealed class CrystalBombBossHooks : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly ISourceEngineContext engine;
        private readonly StrawberryJamModule module;
        private Hook bomb, seeker, puffer;
        private ILHook music, sprite;

        public CrystalBombBossHooks(StrawberryJamModule module, ISourceEngineContext engine)
        {
            this.module = module;
            this.engine = engine;
            try
            {
                bomb = new Hook(typeof(CrystalBomb).GetMethod("Explode", Flags),
                    (Action<Action<CrystalBomb>, CrystalBomb>)BombExplode);
                seeker = new Hook(typeof(OriginalSeekerState).GetMethod("RegenerateCoroutine", Flags),
                    (Func<Func<OriginalSeekerState, IEnumerator>, OriginalSeekerState, IEnumerator>)SeekerRegenerate);
                puffer = new Hook(typeof(OriginalPufferState).GetMethod("Explode", Flags),
                    (Action<Action<OriginalPufferState>, OriginalPufferState>)PufferExplode);
                music = new ILHook(
                    typeof(OriginalFinalBossProgressionPolicy).GetMethod(nameof(OriginalFinalBossProgressionPolicy
                        .AfterHit)), ChangeMusicIl);
                sprite = new ILHook(typeof(FinalBossState).GetMethod("CreateBossSprite", Flags), ChangeSpriteIl);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(SceneEntityState entity) => entity.Scene is OriginalLevelSceneRoot level &&
                                                      ReferenceEquals(level.EngineContext, engine);

        private void BombExplode(Action<CrystalBomb> original, CrystalBomb self)
        {
            if (!Owns(self))
            {
                original(self);
                return;
            }

            if (CrystalBombBossBindings.Exploded(self)) return;
            var previous = self.Collider;
            self.Collider = CrystalBombBossBindings.BombRadius(self);
            HitCollidingBosses(self);
            self.Collider = previous;
            original(self);
        }

        private IEnumerator SeekerRegenerate(Func<OriginalSeekerState, IEnumerator> original, OriginalSeekerState self)
        {
            IEnumerator routine = original(self);
            while (routine.MoveNext()) yield return routine.Current;
            if (!Owns(self)) yield break;
            var previous = self.Collider;
            self.Collider = CrystalBombBossBindings.SeekerRadius(self);
            HitCollidingBosses(self);
            self.Collider = previous;
        }

        private void PufferExplode(Action<OriginalPufferState> original, OriginalPufferState self)
        {
            original(self);
            if (!Owns(self)) return;
            var previous = self.Collider;
            self.Collider = CrystalBombBossBindings.PufferRadius(self);
            HitCollidingBosses(self);
            self.Collider = previous;
        }

        private static void HitCollidingBosses(SceneEntityState source)
        {
            var hits = new List<FinalBossState>();
            var entities =
                ((OriginalLevelSceneRoot)source.Scene).SourceScene.Entities.GetTrackedEntities("Celeste.FinalBoss");
            for (int i = 0; i < entities.Count; i++)
                if (SourceEntityCollision.Check(source, (FinalBossState)entities[i]))
                    hits.Add((FinalBossState)entities[i]);
            // Original CollideAll collects the complete collision snapshot before OnHit mutates boss state.
            for (int i = 0; i < hits.Count; i++)
                if (hits[i] is CrystalBombBadelineBoss boss)
                    boss.OnHit();
        }

        private void ChangeMusicIl(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchLdstr("event:/music/lvl6/badeline_fight")))
            {
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<string, FinalBossState, string>>(ChangeMusic);
            }
        }

        private string ChangeMusic(string original, FinalBossState self) =>
            self is CrystalBombBadelineBoss boss && ReferenceEquals(boss.SourceEngine, engine) &&
            !string.IsNullOrEmpty(boss.Music)
                ? boss.Music
                : original;

        private void ChangeSpriteIl(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                instruction => instruction.MatchCallOrCallvirt<ISpriteFactory>(nameof(ISpriteFactory.Create)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Sprite, FinalBossState, Sprite>>(ChangeSprite);
        }

        private Sprite ChangeSprite(Sprite original, FinalBossState self) =>
            self is CrystalBombBadelineBoss boss && ReferenceEquals(boss.SourceEngine, engine)
                ? module.SpriteBank.Create("crystalBombBadelineBoss")
                : original;

        public void Dispose()
        {
            sprite?.Dispose();
            sprite = null;
            music?.Dispose();
            music = null;
            puffer?.Dispose();
            puffer = null;
            seeker?.Dispose();
            seeker = null;
            bomb?.Dispose();
            bomb = null;
        }
    }
}
