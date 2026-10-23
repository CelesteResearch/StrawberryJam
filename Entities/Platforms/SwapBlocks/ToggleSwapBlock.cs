using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Framework;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Platforms.SwapBlocks.Rendering;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms.SwapBlocks
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ToggleSwapBlock")]
    [CustomEntity("SJ2021/ToggleSwapBlock")]
    [SceneTracked]
    public sealed class ToggleSwapBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new ToggleSwapBlock(level.EngineContext, data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        private const int STAY = 8, DONE = 9;

        private static string[] paths = new string[]
            { "right", "downRight", "down", "downLeft", "left", "upLeft", "up", "upRight", "stay", "done" };

        private static readonly string defaultIndicatorPath =
            "objects/StrawberryJam2021/toggleSwapBlock/indicator/plain/";

        private static readonly string defaultStartAudio = "event:/game/05_mirror_temple/swapblock_move";
        private static readonly string defaultStopAudio = "event:/game/05_mirror_temple/swapblock_move_end";
        private DashListenerState dashListener;
        private ISourceAudioEventReference moveSfx;
        private readonly PackedColor offColor;
        private readonly PackedColor onColor;
        private readonly PackedColor endColor;
        private ToggleBlockLaser[] lasers;

        private readonly int laserCount;

        //private Sprite middleRed;
        private float lerp;
        private AtlasEntry[,] nineSliceBlock;
        private Vector2[] nodes;
        private ToggleBlockNode[] nodeTextures;
        private int nodeIndex = 0;
        private bool moving = false;
        private float travelSpeed = 5f;
        private readonly bool oscillate = false;
        private bool returning = false;
        private readonly bool stopAtEnd = false;
        public Vector2 Direction { get; private set; } = Vector2.Zero;
        public bool Moving => moving;
        public bool AllowDashSliding => allowDashSliding;
        private bool stopped = false;
        private readonly string customTexturePath;

        private readonly bool useIndicators;
        private readonly string indicatorPath;
        private AtlasEntry indicatorTexture;
        private readonly bool allowDashSliding;
        private readonly bool disableTracks;
        private readonly bool isConstant;
        private readonly float constantSpeed;
        private const float vanillaSpeed = 360f;
        private readonly bool accelerate;
        private readonly string startAudio;
        private readonly string stopAudio;
        private float speed;
        private Vector2 targetPosition;
        private Vector2 startPosition;
        private Vector2 lerpVector;

        public ToggleSwapBlock(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world) :
            base(data.Position + offset, data.Width, data.Height, false, world)
        {
            this.engine = engine;
            Depth = -9999;
            nodes = new Vector2[data.Nodes.Length + 1];
            nodes[0] = Position;
            for (int i = 0; i < data.Nodes.Length; i++)
            {
                nodes[i + 1] = data.NodesOffset(offset)[i];
            }

            oscillate = data.Bool("oscillate", false);
            stopAtEnd = data.Bool("stopAtEnd", false);
            customTexturePath = data.Attr("customTexturePath", "");
            allowDashSliding = data.Bool("allowDashSliding", false);
            isConstant = data.Bool("constantSpeed", false);
            travelSpeed = data.Float("travelSpeed", 5f);
            constantSpeed = travelSpeed * vanillaSpeed;
            accelerate = data.Bool("accelerate", false);
            disableTracks = nodes.Length < 2 || data.Bool("disableTracks", false);
            useIndicators = data.Bool("directionIndicator", false);
            startAudio = GetDefaultIfEmpty(data, "customStartAudio", defaultStartAudio);
            stopAudio = GetDefaultIfEmpty(data, "customStopAudio", defaultStopAudio);
            indicatorPath = GetDefaultIfEmpty(data, "customIndicatorPath", defaultIndicatorPath);
            onColor = SourceHexColor.HexToColor(data.Attr("onColor", "9370db")); // PackedColor.MediumPurple
            offColor = SourceHexColor.HexToColor(data.Attr("offColor", "a9a9a9")); // PackedColor.DarkGray
            endColor = SourceHexColor.HexToColor(data.Attr("endColor", "a56666")); // PackedColor(0.65f, 0.4f, 0.4f)
            if (indicatorPath.Last() != '/')
            {
                indicatorPath += '/';
            }

            Add(dashListener = new DashListenerState());
            Add(new LightOccludeState(0.2f));
            dashListener.OnDash = OnPlayerDashed;
            AtlasEntry val3 = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                (customTexturePath.Length <= 0)
                    ? "objects/StrawberryJam2021/toggleSwapBlock/block1"
                    : customTexturePath];
            nineSliceBlock = new AtlasEntry[3, 3];
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    nineSliceBlock[j, k] = val3.GetSubtexture(j * 8, k * 8, 8, 8);
                }
            }

            if (!disableTracks)
            {
                laserCount = (oscillate || stopAtEnd || nodes.Length <= 2) ? nodes.Length - 1 : nodes.Length;
                lasers = new ToggleBlockLaser[laserCount];
                nodeTextures = new ToggleBlockNode[nodes.Length];
            }
        }

        private string GetDefaultIfEmpty(EntityData data, string attrName, string defaultAttr)
        {
            string attr = data.Attr(attrName, "");
            return attr.Equals("") ? defaultAttr : attr;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!disableTracks)
            {
                Vector2 radius = new Vector2(Width, Height) / 2f;
                if (nodes.Length != 2)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        ((ISourceScene)Scene).SourceScene.QueueSourceEntity(lasers[i] = engine.EntityPools
                            .Create<ToggleBlockLaser>().Init(nodes[i] + radius, nodes[GetNextNode(i)] + radius));
                    }
                }
                else
                {
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(lasers[0] = engine.EntityPools
                        .Create<ToggleBlockLaser>().Init(nodes[0] + radius, nodes[1] + radius));
                }

                for (int j = 0; j < nodes.Length; j++)
                {
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(nodeTextures[j] =
                        engine.EntityPools.Create<ToggleBlockNode>().Init(nodes[j] + radius));
                }
            }

            UpdateNextNode();
        }

        private int GetNextNode(int node)
        {
            if (!oscillate && node == nodes.Length - 1)
            {
                return 0;
            }

            if (!oscillate)
            {
                return node + 1;
            }

            if (node == nodes.Length - 1)
            {
                return node - 1;
            }

            if (node != 0)
            {
                return node + ((!returning) ? 1 : (-1));
            }

            return 1;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (moveSfx != null)
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
            }
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (moveSfx != null)
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
            }
        }

        public override void Update()
        {
            base.Update();

            if (moving)
            {
                if (Position != targetPosition)
                {
                    speed = ScalarMath.Approach(speed, travelSpeed, travelSpeed / .2f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    lerp = ScalarMath.Approach(lerp, 1f, speed * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    MoveTo(
                        Vector2Interpolation.Lerp(startPosition, targetPosition, lerp,
                            ((ISourceScalarEnvironment)engine).ScalarArithmetic), lerpVector);
                }
                else
                {
                    lerp = 0f;
                    ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(Center, 0.2f, 0f, 16f, 1f, null, null);
                    moving = false;
                    //middleRed.Play("idle", false, false);
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Stop(moveSfx, true);
                    moveSfx = null;
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(stopAudio, Center);
                    UpdateNextNode();
                }
            }
        }

        private void UpdateNextNode()
        {
            Vector2 dir = nodes[GetNextNode(nodeIndex)] - nodes[nodeIndex];

            if (isConstant)
            {
                travelSpeed = constantSpeed / ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(dir);
            }

            if (useIndicators)
            {
                int indicator;
                if (stopAtEnd && nodeIndex == nodes.Length - 1)
                {
                    indicator = DONE;
                }
                else
                {
                    if (dir.Equals(Vector2.Zero))
                    {
                        indicator = STAY;
                    }
                    else
                    {
                        indicator = (int)Math.Round(Vector2Geometry.Angle(dir) * (4 / Math.PI));
                        if (indicator < 0)
                        {
                            indicator += 8;
                        }
                    }
                }

                indicatorTexture =
                    ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[indicatorPath + paths[indicator]];
            }

            if (!disableTracks)
            {
                Vector2 radius = new Vector2(Width, Height) / 2f;
                for (int i = 0; i < laserCount; i++)
                {
                    if (lasers[i].Start == nodes[nodeIndex] + radius &&
                        lasers[i].End == nodes[GetNextNode(nodeIndex)] + radius)
                    {
                        lasers[i].Color = onColor;
                    }
                    else if (lasers[i].End == nodes[nodeIndex] + radius &&
                             lasers[i].Start == nodes[GetNextNode(nodeIndex)] + radius)
                    {
                        lasers[i].Color = onColor;
                    }
                    else
                    {
                        lasers[i].Color = offColor;
                    }

                    if (stopAtEnd && i == laserCount - 1)
                    {
                        lasers[i].Color = endColor;
                    }
                }
            }
        }

        private void OnPlayerDashed(Vector2 direction)
        {
            if (moving || stopped)
            {
                if (stopped)
                {
                    StartShaking(0.25f);
                }

                return;
            }

            moving = true;
            if (nodeIndex < nodes.Length - 1 && nodeIndex != 0)
            {
                if (!returning)
                {
                    nodeIndex++;
                }
                else
                {
                    nodeIndex--;
                }
            }
            else if (nodeIndex == 0)
            {
                if (returning)
                {
                    returning = false;
                }

                nodeIndex++;
            }
            else
            {
                if (stopAtEnd)
                {
                    nodeIndex = -1;
                    stopped = true;
                    moving = false;
                    StartShaking(0.5f);
                    return;
                }

                if (oscillate)
                {
                    nodeIndex--;
                    returning = true;
                }
                else
                {
                    nodeIndex = 0;
                }
            }

            //middleRed.Play("moving", false, false);
            moveSfx = ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(startAudio, Center);
            targetPosition = nodes[nodeIndex];
            startPosition = Position;
            Direction = targetPosition - startPosition;
            lerpVector = Direction * travelSpeed;


            speed = accelerate
                ? ((ISourceScalarEnvironment)engine).ScalarArithmetic.Lerp(travelSpeed * .333f, travelSpeed, 0f)
                : travelSpeed;
        }

        public override void Render(ISourceRenderContext context)
        {
            int num = (oscillate || nodeIndex != nodes.Length - 1)
                ? ((!oscillate)
                    ? (nodeIndex + 1)
                    : ((nodeIndex == nodes.Length - 1)
                        ? (nodeIndex - 1)
                        : ((nodeIndex == 0) ? 1 : (nodeIndex + ((!returning) ? 1 : (-1))))))
                : 0;
            if (!disableTracks)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (stopAtEnd && i == nodes.Length - 1)
                    {
                        nodeTextures[i].Color = endColor;
                    }
                    else if (stopAtEnd && (i == 0 || (oscillate && i == nodes.Length - 2)))
                    {
                        nodeTextures[i].Color = offColor;
                    }
                    else if (moving || stopped)
                    {
                        nodeTextures[i].Color = offColor;
                    }
                    else
                    {
                        nodeTextures[i].Color = ((num == i) ? onColor : offColor);
                    }
                }
            }

            SwapBlockDrawing.DrawBlockStyle(context, this, Position + Shake, Width, Height, nineSliceBlock, null,
                PackedColor.White);
            if (useIndicators)
                SourceAtlasDrawing.DrawCentered(context, this, indicatorTexture, Position + Shake + Center - Position,
                    PackedColor.White);
        }
    }
}
