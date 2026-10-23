using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.StrawberryJam.Rendering.Outlines
{
    public sealed class MaskedOutlineResources
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, MaskedOutlineResources> owners = new();

        public static MaskedOutlineResources For(ISourceEngineContext engine) =>
            owners.GetValue(engine, _ => new MaskedOutlineResources());

        public Vector2[] BoosterDots, RefillDots, DoubleRefillDots;
        public AtlasEntry DotTexture;
        public PackedColor SingleColor, DoubleColor;

        public static void Load(ISourceEngineContext engine)
        {
            var r = For(engine);
            r.BoosterDots = DotsFromOffsets(new[] { new[] { 9, 8, 7, 5, 3, 1 }, new[] { 2, 4, 6, 8, 9, 9 } });
            r.RefillDots = DotsFromOffsets(new[] { new[] { 5, 3, 1 }, new[] { 1, 3, 5 } });
            r.DoubleRefillDots = DotsFromOffsets(new[] { new[] { 3, 4, 4, 2, 1 }, new[] { 1, 2, 3, 5, 6 } });
            r.SingleColor = SourceHexColor.HexToColor("2ad257");
            r.DoubleColor = SourceHexColor.HexToColor("d47df9");
        }

        public static void LoadTexture(ISourceEngineContext engine) => For(engine).DotTexture =
            ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["util/pixel"].GetSubtexture(1, 1, 1, 1);

        private static Vector2[] DotsFromOffsets(int[][] offsets)
        {
            var result = new Vector2[offsets[0].Length * 4];
            for (int i = 0; i < offsets[0].Length; i++)
            {
                int index = i * 4;
                result[index] = new Vector2(offsets[0][i], offsets[1][i]);
                result[index + 1] = new Vector2(1 - offsets[0][i], offsets[1][i]);
                result[index + 2] = new Vector2(1 - offsets[0][i], 1 - offsets[1][i]);
                result[index + 3] = new Vector2(offsets[0][i], 1 - offsets[1][i]);
            }

            return reorderArray(result);
        }

        private static Vector2[] reorderArray(Vector2[] arr)
        {
            int groups = arr.Length / 4;
            int offset = 0;
            var result = new Vector2[arr.Length];
            int index = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < groups; j++) result[index++] = arr[j * 4 + offset];
                offset++;
                for (int j = groups - 1; j >= 0; j--) result[index++] = arr[j * 4 + offset];
                offset++;
            }

            return result;
        }
    }
}
