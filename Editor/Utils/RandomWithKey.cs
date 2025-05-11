using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class RandomWithKey : IRandom
    {
        private const long a = 1664525;
        private const long c = 1013904223;
        private const long m = 4294967296; // 2^32

        private readonly int[] _key;

        private int _nextIndex;

        private int _seed;

        public RandomWithKey(byte[] key, int seed)
        {
            _key = ConvertToIntKey(key);
            _seed = seed;
        }

        private static int[] ConvertToIntKey(byte[] key)
        {
            // ignore last bytes if not aligned to 4
            int align4Length = key.Length / 4;
            int[] intKey = new int[align4Length];
            Buffer.BlockCopy(key, 0, intKey, 0, align4Length * 4);
            return intKey;
        }

        public int NextInt(int min, int max)
        {
            return min + NextInt(max - min);
        }

        public int NextInt(int max)
        {
            return (int)((uint)NextInt() % (uint)max);
        }

        private int GetNextSalt()
        {
            if (_nextIndex >= _key.Length)
            {
                _nextIndex = 0;
            }
            return _key[_nextIndex++];
        }

        public int NextInt()
        {
            _seed = (int)((a * _seed + c) % m);
            return _seed ^ GetNextSalt();
        }

        public long NextLong()
        {
            return ((long)NextInt() << 32) | (uint)NextInt();
        }
    }
}
