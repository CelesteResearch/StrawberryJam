using System.Runtime.CompilerServices;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.StrawberryJam.Entities.Platforms
{
    internal sealed class LoopBlockTextures
    {
        private static readonly ConditionalWeakTable<AtlasData, LoopBlockTextures> cache = new();
        public readonly AtlasEntry[,,] OuterEdges = new AtlasEntry[3, 3, 3];
        public readonly AtlasEntry[,,] WallEdges = new AtlasEntry[3, 2, 3];
        public readonly AtlasEntry[,] InnerCorners = new AtlasEntry[2, 2];
        public readonly AtlasEntry[] CenterTiles = new AtlasEntry[8];

        public static LoopBlockTextures Get(AtlasData atlas) =>
            cache.GetValue(atlas, value => new LoopBlockTextures(value));

        private LoopBlockTextures(AtlasData atlas)
        {
            AtlasEntry tiles = atlas.Entries["objects/StrawberryJam2021/loopBlock/tiles"];
            for (int i = 0; i < 3; i++)
            {
                int tx = i * 8;

                OuterEdges[0, 0, i] = tiles.GetSubtexture(tx, 0, 8, 8); // outer top left
                OuterEdges[2, 0, i] = tiles.GetSubtexture(24 + tx, 0, 8, 8); // outer top right
                OuterEdges[0, 2, i] = tiles.GetSubtexture(tx, 8, 8, 8); // outer bottom left
                OuterEdges[2, 2, i] = tiles.GetSubtexture(24 + tx, 8, 8, 8); // outer bottom right
                OuterEdges[1, 0, i] = tiles.GetSubtexture(tx, 16, 8, 8); // outer top
                OuterEdges[1, 2, i] = tiles.GetSubtexture(tx, 24, 8, 8); // outer bottom
                OuterEdges[0, 1, i] = tiles.GetSubtexture(24 + tx, 16, 8, 8); // outer left
                OuterEdges[2, 1, i] = tiles.GetSubtexture(24 + tx, 24, 8, 8); // outer right

                WallEdges[0, 0, i] = tiles.GetSubtexture(tx, 32, 8, 8); // outer inner top left
                WallEdges[1, 0, i] = tiles.GetSubtexture(24 + tx, 32, 8, 8); // outer inner top right
                WallEdges[0, 1, i] = tiles.GetSubtexture(tx, 40, 8, 8); // outer inner bottom left
                WallEdges[1, 1, i] = tiles.GetSubtexture(24 + tx, 40, 8, 8); // outer inner bottom right
                WallEdges[2, 0, i] = tiles.GetSubtexture(tx, 48, 8, 8); // outer inner horizontal
                WallEdges[2, 1, i] = tiles.GetSubtexture(24 + tx, 48, 8, 8); // outer inner vertical

                if (i > 0)
                {
                    CenterTiles[i - 1] = tiles.GetSubtexture(tx, 56, 8, 8); // center 0, 1
                    CenterTiles[i + 1] = tiles.GetSubtexture(24 + tx, 56, 8, 8); // center 2, 3
                    CenterTiles[i + 3] = tiles.GetSubtexture(tx, 64, 8, 8); // center 4, 5
                    CenterTiles[i + 5] = tiles.GetSubtexture(24 + tx, 64, 8, 8); // center 6, 7
                }
            }

            InnerCorners[0, 0] = tiles.GetSubtexture(0, 56, 8, 8); // inner top left
            InnerCorners[1, 0] = tiles.GetSubtexture(24, 56, 8, 8); // inner top right
            InnerCorners[0, 1] = tiles.GetSubtexture(0, 64, 8, 8); // inner bottom left
            InnerCorners[1, 1] = tiles.GetSubtexture(24, 64, 8, 8); // inner bottom right
        }
    }
}
