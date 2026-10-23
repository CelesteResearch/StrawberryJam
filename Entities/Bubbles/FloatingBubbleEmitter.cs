using System.Collections;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.StrawberryJam.Runtime.Bubbles;

namespace Celeste.Mods.StrawberryJam.Entities.Bubbles
{
    [SceneTracked, CustomEntity("SJ2021/FloatingBubbleEmitter")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.FloatingBubbleEmitter")]
    public class FloatingBubbleEmitter : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly FloatingBubbleProcess owner;
        private readonly Sprite sprite;
        private bool firing;
        public string flag;

        public FloatingBubbleEmitter(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            owner = FloatingBubbleProcess.For(engine);
            flag = data.Attr("flag");
            Add(sprite = owner.Module.SpriteBank.Create("bubbleEmitter"));
            sprite.CenterOrigin();
        }

        public void Fire()
        {
            if (!firing) Add(new SimulationCoroutine(engine, SpawnRoutine()));
        }

        private IEnumerator SpawnRoutine()
        {
            firing = true;
            sprite.Play("open");
            while (sprite.CurrentAnimationFrame != 1) yield return null;
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new FloatingBubble(owner,
                new Vector2(Position.X, Position.Y - 18)));
            ((IOriginalGameplayProcess)engine).AudioSession.Play(
                "event:/strawberry_jam_2021/game/bubble_emitter/emitter_generate", Position,
                ((IOriginalGameplayProcess)engine).SaveContext);
            yield return null;
            firing = false;
        }

        public static FloatingBubbleEmitter Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level.EngineContext, data, offset);
    }
}
