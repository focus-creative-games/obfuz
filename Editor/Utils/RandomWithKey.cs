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
        private readonly byte[] _key;

        private readonly Random _random;

        private int _nextIndex;

        public RandomWithKey(byte[] key, int seed)
        {
            _key = key;
            // TODO use key and seed to generate a random number
            _random = new Random(GenerateSeed(key, seed));
        }

        private int GenerateSeed(byte[] key, int seed)
        {
            foreach (var b in key)
            {
                seed = seed * 31 + b;
            }
            return seed;
        }

        public int NextInt(int min, int max)
        {
            return min + NextInt(max - min);
        }

        public int NextInt(int max)
        {
            return (int)((uint)NextInt() % (uint)max);
        }

        private int GetNextKeyByte()
        {
            if (_nextIndex >= _key.Length)
            {
                _nextIndex = 0;
            }
            return _key[_nextIndex++];
        }

        public int NextInt()
        {
            return _random.Next() ^ GetNextKeyByte();
        }

        public long NextLong()
        {
            return ((long)NextInt() << 32) | (uint)NextInt();
        }
    }
}
