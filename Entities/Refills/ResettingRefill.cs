using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Mods.ExtendedVariants.Configuration;
using Celeste.Mods.ExtendedVariants.Runtime;
using Celeste.Mods.ExtendedVariants.Runtime.JumpVariants;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.StrawberryJam.Entities.Refills
{
    [SceneTracked]
    [CustomEntity("SJ2021/ResettingRefill")]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ResettingRefill")]
    public class ResettingRefill : OriginalRefillState
    {
        private readonly int dashes;
        private readonly bool extraJump, persistJump;

        public ResettingRefill(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Int("dashes") == 2, data.Bool("oneUse"), (IOriginalRefillProcess)engine)
        {
            dashes = data.Int("dashes");
            extraJump = data.Bool("extraJump");
            persistJump = data.Bool("persistJump");
            Remove(Get<PlayerColliderComponentState>());
            if (extraJump)
            {
                string texture = persistJump ? "ExtendedVariantMode/jumprefill" : "ExtendedVariantMode/jumprefillblue";
                outline.Texture = this.engine.GameGraphics.GameAtlas[$"objects/{texture}/outline"];
                sprite.Reset(this.engine.GameGraphics.GameAtlas, $"objects/{texture}/idle");
                sprite.AddLoop("idle", "", .1f);
                sprite.Play("idle");
                flash.Reset(this.engine.GameGraphics.GameAtlas, $"objects/{texture}/flash");
                flash.Add("flash", "", .05f);
                flash.OnFinish = delegate { flash.Visible = false; };
            }

            Add(new PlayerColliderComponentState(OnPlayerRR));
        }

        private void OnPlayerRR(RoomPlayerSession player)
        {
            var context = (ISourceEngineContext)engine;
            ((OriginalLevelSceneRoot)Scene).Session.Inventory.Dashes = dashes;
            player.Data.Dashes = dashes;
            VariantRuntime.For(context).Enter(Variant.JumpCount, persistJump ? 2 : 1, false, false, true);
            JumpVariantProcess.For(context).Add(extraJump ? 1 : 0, true, extraJump ? 1 : 0);
            player.RefillStamina();
            if (dashes is 0 or 1)
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/general/diamond_touch");
            else if (dashes == 2)
                ((IOriginalGameplayProcess)engine).AudioSession.Play(
                    "event:/new_content/game/10_farewell/pinkdiamond_touch");
            engine.RumbleSession.Rumble(1, 1);
            Collidable = false;
            Add(new SimulationCoroutine(engine, RefillRoutine(player)));
            respawnTimer = 2.5f;
        }

        public static ResettingRefill Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset);
    }
}
