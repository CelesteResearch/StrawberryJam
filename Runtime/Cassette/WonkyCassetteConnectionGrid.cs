using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.StrawberryJam.Runtime.Cassette
{
    internal sealed class WonkyCassetteConnectionGrid
    {
        private readonly int width;
        private readonly int height;
        private readonly uint[] words;
        private readonly bool[,] large;

        internal WonkyCassetteConnectionGrid(int width, int height)
        {
            this.width = width;
            this.height = height;
            long cells = (long)width * height;
            if (width < 0 || height < 0 || cells > int.MaxValue)
            {
                large = new bool[width, height];
                return;
            }

            words = new uint[(int)((cells + 31) / 32)];
        }

        internal bool this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (large != null) return large[x, y];
                if ((uint)x >= (uint)width || (uint)y >= (uint)height) throw new IndexOutOfRangeException();
                int index = x * height + y;
                return (words[index >> 5] & (1u << (index & 31))) != 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (large != null)
                {
                    large[x, y] = value;
                    return;
                }

                if ((uint)x >= (uint)width || (uint)y >= (uint)height) throw new IndexOutOfRangeException();
                int index = x * height + y;
                uint mask = 1u << (index & 31);
                if (value) words[index >> 5] |= mask;
                else words[index >> 5] &= ~mask;
            }
        }
    }
}
