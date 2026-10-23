using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.StrawberryJam.Rendering.Masks
{
    [SceneTracked(true)]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.StylegroundMasks.Mask")]
    public class Mask : SceneEntityState
    {
        public static readonly SourceBlendState DestinationAlphaBlend = new()
        {
            ColorSourceBlend = SourceBlendFactor.DestinationAlpha,
            ColorDestinationBlend = SourceBlendFactor.InverseSourceAlpha,
            AlphaSourceBlend = SourceBlendFactor.Zero, AlphaDestinationBlend = SourceBlendFactor.One,
            AlphaBlendFunction = SourceBlendFunction.Add
        };

        public static readonly SourceBlendState DestinationAlphaSourceColorBlend = new()
        {
            ColorSourceBlend = SourceBlendFactor.One, ColorDestinationBlend = SourceBlendFactor.Zero,
            ColorBlendFunction = SourceBlendFunction.Add, AlphaSourceBlend = SourceBlendFactor.Zero,
            AlphaDestinationBlend = SourceBlendFactor.One, AlphaBlendFunction = SourceBlendFunction.Add
        };

        public MaskFadeType Fade { get; set; }
        public AtlasEntry FadeMask { get; set; }
        public string Flag { get; set; }
        public bool NotFlag { get; set; }
        public float ScrollX { get; set; }
        public float ScrollY { get; set; }
        public OriginalLevelSceneRoot Level;
        protected readonly ISourceEngineContext engine;
        protected Vector2 startPosition;
        private readonly List<MaskSlice> slices = new(320);

        public Mask(ISourceEngineContext engine, Vector2 position, float width, float height)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            Collider = new HitboxColliderGeometry(width, height);
            startPosition = Position;
        }

        public Mask(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Width, data.Height)
        {
            Fade = data.Enum("fade", MaskFadeType.None);
            FadeMask = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                .GameAtlas.GetOrDefault("fademasks/" + data.Attr("customFade"), null);
            Flag = data.Attr("flag");
            NotFlag = data.Bool("notFlag");
            ScrollX = data.Float("scrollX");
            ScrollY = data.Float("scrollY");
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Level = (OriginalLevelSceneRoot)scene;
        }

        public override void Render(ISourceRenderContext context)
        {
            var cameraCenter = Level.Camera.Position + new Vector2(160f, 90f);
            if (ScrollX != 0f || ScrollY != 0f)
            {
                var center = startPosition + new Vector2(Width / 2f, Height / 2f);
                var offset = center + (center - cameraCenter) * new Vector2(ScrollX, ScrollY);
                Center = new Vector2((float)Math.Round(offset.X), (float)Math.Round(offset.Y));
            }

            base.Render(context);
        }

        public void DrawFadeMask(PackedColor? color = null)
        {
            if (FadeMask != null)
                SourceAtlasDrawing.Draw(Level.SourceRenderContext, this, FadeMask, Position, Vector2.Zero,
                    color ?? PackedColor.White,
                    new Vector2(Width / FadeMask.FrameWidth, Height / FadeMask.FrameHeight));
        }

        public bool IsVisible()
        {
            if (!string.IsNullOrEmpty(Flag) && Level.Session.GetFlag(Flag) == NotFlag) return false;
            var rectangle = GetVisibleRect();
            return rectangle.Width > 0 && rectangle.Height > 0;
        }

        public IntRect GetVisibleRect()
        {
            var rectangle = new IntRect((int)(X - Level.Camera.X), (int)(Y - Level.Camera.Y), (int)Width, (int)Height);
            int left = Math.Max(0, rectangle.Left), top = Math.Max(0, rectangle.Top);
            int right = Math.Min(320, rectangle.Right), bottom = Math.Min(180, rectangle.Bottom);
            return right > left && bottom > top ? new IntRect(left, top, right - left, bottom - top) : IntRect.Empty;
        }

        public Vector2 GetDrawPos()
        {
            var source = GetVisibleRect();
            return new Vector2(source.X + Level.Camera.X, source.Y + Level.Camera.Y);
        }

        public Vector2 GetDrawOffset() => new(Math.Max(0, Level.Camera.X - X), Math.Max(0, Level.Camera.Y - Y));

        public List<MaskSlice> GetMaskSlices()
        {
            slices.Clear();
            if (!IsVisible()) return slices;
            var source = GetVisibleRect();
            if (Fade is MaskFadeType.None or MaskFadeType.Custom) slices.Add(new MaskSlice(GetDrawPos(), source));
            else
            {
                var offset = GetDrawOffset();
                if (Fade is MaskFadeType.LeftToRight or MaskFadeType.RightToLeft)
                    for (int x = (int)offset.X; x < Width && x - (int)offset.X < source.Width; x++)
                        slices.Add(new MaskSlice(Position + new Vector2(x, offset.Y),
                            new IntRect(source.X + x - (int)offset.X, source.Y, 1, source.Height),
                            Fade == MaskFadeType.LeftToRight ? x / Width : 1 - x / Width));
                else
                    for (int y = (int)offset.Y; y < Height && y - (int)offset.Y < source.Height; y++)
                        slices.Add(new MaskSlice(Position + new Vector2(offset.X, y),
                            new IntRect(source.X, source.Y + y - (int)offset.Y, source.Width, 1),
                            Fade == MaskFadeType.TopToBottom ? y / Height : 1 - y / Height));
            }

            return slices;
        }
    }
}
