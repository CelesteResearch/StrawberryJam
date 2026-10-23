using Celeste.Core.Reflection.Runtime;
using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Gameplay.Platforms.Clouds;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.StrawberryJam.Entities
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.NodedCloud")]
    [CustomEntity("SJ2021/NodedCloud")]
    public sealed class NodedCloud : OriginalCloudState
    {
        private const byte BaseGhostOpacity = 0x62;
        private readonly ISourceEngineContext engine;
        private readonly Vector2[] nodes;
        private readonly Vector2 roomOffset;
        private readonly float moveTime;
        private readonly SpriteImage ghost;
        private readonly ParticleTypePresentation spawnParticle;
        private readonly ParticleTypePresentation ghostParticle;
        private int nextNode;
        private float fadeInProgress = 1f;

        public NodedCloud(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world)
            : base(engine, data.Position + offset, true, world)
        {
            this.engine = engine;
            roomOffset = offset;
            moveTime = data.Float("moveTime", 0.5f);
            nodes = data.Nodes;
            var graphics = (OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics;
            ghost = new SpriteImage(graphics.GameAtlas.Entries["objects/clouds/fragile00"]);
            ghost.CenterOrigin();
            ghost.Color = new PackedColor(0, 0, 0, BaseGhostOpacity);
            Add(ghost);
            var fragileCloud = ((IOriginalGameplayProcess)engine).ParticleTypes.Cloud.P_FragileCloud;
            var particles = NodedCloudParticles.For(fragileCloud);
            spawnParticle = particles.Spawn;
            ghostParticle = particles.Ghost;
            Add(new SimulationCoroutine(engine, MoveRoutine()));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData data)
            => new NodedCloud(level.EngineContext, data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.CreateJumpThru(data.Position + offset, false));

        public override void Render(ISourceRenderContext context)
        {
            if (nextNode < nodes.Length)
            {
                ghost.RenderPosition = nodes[nextNode] + roomOffset;
                ghost.Color.A = (byte)(fadeInProgress * BaseGhostOpacity * (1 + 0.2f * (float)Math.Sin(timer * 4)));
            }
            else
            {
                ghost.Visible = false;
            }

            base.Render(context);
        }

        private IEnumerator MoveRoutine()
        {
            while (nextNode + 1 <= nodes.Length)
            {
                while (Collidable) yield return null;
                yield return 0.4f;
                startY = (nodes[nextNode] + roomOffset).Y;
                respawnTimer = Math.Max(moveTime - 0.4f, 0f);
                Position = nodes[nextNode] + roomOffset;
                fadeInProgress = 0;
                Add(new SimulationCoroutine(engine, GhostFadeInRoutine()));
                nextNode++;
                while (!Collidable)
                {
                    if (((OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene).OnInterval(0.05f) &&
                        nextNode <= nodes.Length)
                        ((OriginalLevelSceneRoot)Scene).ParticlesBG.Emit(spawnParticle, 5,
                            nodes[nextNode - 1] + roomOffset,
                            new Vector2(Collider.Width / 2f, 5f), (float)Math.PI / 2);
                    yield return null;
                }
            }

            while (Collidable) yield return null;
            yield return 1f;
            RemoveSelf();
        }

        private IEnumerator GhostFadeInRoutine()
        {
            yield return moveTime / 2;
            while (fadeInProgress < 1)
            {
                fadeInProgress += engine.DeltaTime * 2f * moveTime;
                if (nextNode + 1 <= nodes.Length &&
                    ((OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene).OnInterval(0.05f))
                    ((OriginalLevelSceneRoot)Scene).ParticlesBG.Emit(ghostParticle, 5, nodes[nextNode] + roomOffset,
                        new Vector2(Collider.Width / 2f, 5f), (float)Math.PI / 2);
                yield return null;
            }

            fadeInProgress = 1;
        }
    }
}
