using Celeste.Scene.Tracking;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Rendering;
using Celeste.Mods.StrawberryJam.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.StrawberryJam.Entities.Strawberries
{
    [CustomEntity("SJ2021/ExplodingStrawberry")]
    [RegisterStrawberry(true, false)]
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Entities.ExplodingStrawberry")]
    public class ExplodingStrawberry : OriginalStrawberryState
    {
        private static readonly Func<OriginalStrawberryState, Sprite> getSprite = BindSprite();
        private readonly IOriginalStrawberryProcess process;
        private Sprite explosionSprite;
        private Vector2 lastPlayerPos;

        private static Func<OriginalStrawberryState, Sprite> BindSprite()
        {
            var method = new DynamicMethod("ExplodingStrawberrySprite", typeof(Sprite),
                new[] { typeof(OriginalStrawberryState) }, typeof(ExplodingStrawberry), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(OriginalStrawberryState).GetField("sprite", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Func<OriginalStrawberryState, Sprite>)method.CreateDelegate(
                typeof(Func<OriginalStrawberryState, Sprite>));
        }

        public ExplodingStrawberry(EntityData data, Vector2 offset, MapEntityId id, IOriginalStrawberryProcess process)
            : base(data, offset, id, process)
        {
            this.process = process;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new ExplodingStrawberry(data, offset, new MapEntityId(room.Name, data.ID),
            (IOriginalStrawberryProcess)level.EngineContext);

        public override void Added(object scene)
        {
            base.Added(scene);
            explosionSprite = ((ISourceEverestModulesEnvironment)process).Modules.OfType<StrawberryJamModule>().Single()
                .SpriteBank.Create("explodingStrawberry");
            explosionSprite.Visible = false;
            Add(explosionSprite);
        }

        public override void Update()
        {
            base.Update();
            var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null) lastPlayerPos = player.Center;
        }

        public override void Render(ISourceRenderContext context)
        {
            float num = 1f;
            Sprite sprite = getSprite(this);
            if (explosionSprite.Visible)
                num = 1f - (float)Math.Pow(
                    (double)explosionSprite.CurrentAnimationFrame / explosionSprite.CurrentAnimationTotalFrames, 2.0);
            else if (sprite.CurrentAnimationID == "collect")
                num = 1f - (float)Math.Pow((double)sprite.CurrentAnimationFrame / sprite.CurrentAnimationTotalFrames,
                    2.0);
            if (num > 0f)
            {
                bool flag = false;
                if (lastPlayerPos.Y < Y)
                {
                    lastPlayerPos.Y = Y - (float)((double)(lastPlayerPos.Y - Y) * .5);
                    lastPlayerPos.X += lastPlayerPos.X - X;
                    flag = true;
                }

                float angle = Vector2Geometry.Angle(lastPlayerPos - Position);
                for (int i = 0; i < 14; i++)
                {
                    float wave = (float)Math.Sin((double)((ISourceScene)Scene).SourceScene.Clock.TimeActive * .5) *
                                 .02f;
                    float radians = ScalarMath.Map((float)i / 14f + wave, 0f, 1f, (float)Math.PI / 30f, 3.2463124f);
                    Vector2 unit = Vector2Geometry.AngleToVector(radians, 1f);
                    Vector2 point = Position - new Vector2(0f, 1.5f) + unit * 16f;
                    if (i == 0 || i == 13)
                    {
                        SourcePrimitiveDrawing.Line(context, this, point, point - unit * 7.5f, PackedColor.White * num,
                            process.VectorArithmetic);
                        continue;
                    }

                    Vector2 delta = unit *
                                    (float)Math.Sin(((ISourceScene)Scene).SourceScene.Clock.TimeActive * 2f +
                                                    (float)i * .6f);
                    if (i % 2 == 0) delta *= -1f;
                    Vector2 p = point + delta;
                    if (!flag && ScalarMath.AbsAngleDiff(radians, angle) <= .17453292f)
                        SourcePrimitiveDrawing.Line(context, this, p, p - unit * 3f, PackedColor.White * num,
                            process.VectorArithmetic);
                    else SourcePrimitiveDrawing.Point(context, this, p, PackedColor.White * num);
                }
            }

            OwnedComponents.Render(context);
        }

        internal void BeginExplosion()
        {
            getSprite(this).Visible = false;
            explosionSprite.Visible = true;
            Add(new SimulationCoroutine(process, Explode(), true));
        }

        private IEnumerator Explode()
        {
            explosionSprite.Play(process.CheckStrawberry(ID) ? "ghostexplode" : "explode", false, false);
            while (explosionSprite.Animating)
            {
                if (Follower.Leader != null)
                {
                    getSprite(this).Visible = true;
                    explosionSprite.Visible = false;
                    yield break;
                }

                yield return null;
            }

            RemoveSelf();
        }
    }
}
