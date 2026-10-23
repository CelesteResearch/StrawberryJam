using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.StrawberryJam.Backdrops.Light
{
    [RuntimeTypeAlias("Celeste.Mod.StrawberryJam2021.Effects.HexagonalGodray")]
    public partial class HexagonalGodray : BackdropPresentation
    {
        private readonly ISourceEngineContext engine;
        private readonly HexRay[] rays;
        private readonly SourceVertexPositionColor[] vertices;
        private int vertexCount;
        private PackedColor rayColor;
        private PackedColor fadeColor;
        private float fade;
        public float speedX, speedY;
        private float minRotation, maxRotation;
        private ISourceRandom Random => ((ISourceRandomEnvironment)engine).Random;

        public HexagonalGodray(ISourceEngineContext engine, string color, string fadeToColor, int numRays,
            float speedx, float speedy, float minRotation, float maxRotation)
        {
            this.engine = engine;
            vertices = new SourceVertexPositionColor[12 * numRays];
            rays = new HexRay[numRays];
            if (string.IsNullOrEmpty(fadeToColor)) fadeToColor = color;
            rayColor = SourceHexColor.HexToColor(color) * .5f;
            fadeColor = SourceHexColor.HexToColor(fadeToColor) * .5f;
            UseSpritebatch = false;
            speedX = speedx;
            speedY = speedy;
            this.minRotation = minRotation;
            this.maxRotation = Math.Max(0f, maxRotation);
            for (int i = 0; i < rays.Length; i++)
            {
                rays[i] = default;
                rays[i].Reset(Random, minRotation, maxRotation);
                rays[i].Percent = SourceRandomMath.NextFloat(Random);
            }
        }

        public override void Update(ISourceBackdropScene scene)
        {
            bool visible = IsVisible(scene);
            fade = ScalarMath.Approach(fade, visible ? 1 : 0, engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            Visible = fade > 0f;
            if (!Visible) return;
            var player = scene.FirstPlayer;
            int index = 0;
            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i].Percent >= 1f) rays[i].Reset(Random, minRotation, maxRotation);
                rays[i].Percent += engine.DeltaTime / rays[i].Duration;
                rays[i].X += speedX * engine.DeltaTime;
                rays[i].Y += speedY * engine.DeltaTime;
                float percent = rays[i].Percent;
                float x = -32f + Mod(rays[i].X - scene.Camera.X * .9f, 384f);
                float y = -32f + Mod(rays[i].Y - scene.Camera.Y * .9f, 244f);
                int length = rays[i].length;
                var at = new Vector2((float)Math.Floor(x), (float)Math.Floor(y));
                var color = PackedColor.Lerp(rayColor, fadeColor, percent) *
                            ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(
                                ScalarMath.Clamp((percent < .5f ? percent : 1f - percent) * 2f, 0f, 1f)) * fade;
                if (player != null)
                {
                    float distance = ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(
                        at + scene.Camera.Position - player.Position);
                    if (distance < 64f) color *= .25f + .75f * (distance / 64f);
                }

                var a = new SourceVertexPositionColor(new Vector3(at + rays[i].points[0] * length, 0f), color);
                var b = new SourceVertexPositionColor(new Vector3(at + rays[i].points[1] * length, 0f), color);
                var c = new SourceVertexPositionColor(new Vector3(at + rays[i].points[2] * length, 0f), color);
                var d = new SourceVertexPositionColor(new Vector3(at - rays[i].points[0] * length, 0f), color);
                var e = new SourceVertexPositionColor(new Vector3(at - rays[i].points[1] * length, 0f), color);
                var f = new SourceVertexPositionColor(new Vector3(at - rays[i].points[2] * length, 0f), color);
                vertices[index++] = a;
                vertices[index++] = b;
                vertices[index++] = c;
                vertices[index++] = a;
                vertices[index++] = c;
                vertices[index++] = d;
                vertices[index++] = a;
                vertices[index++] = d;
                vertices[index++] = e;
                vertices[index++] = a;
                vertices[index++] = e;
                vertices[index++] = f;
            }

            vertexCount = index;
        }

        private float Mod(float x, float m) => (x % m + m) % m;

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            if (vertexCount > 0 && fade > 0f)
                SourcePrimitiveGraphics.DrawVertices(((ISourcePrimitiveGraphicsEnvironment)engine).PrimitiveGraphics,
                    Matrix4x4.Identity, vertices, vertexCount);
        }
    }
}
