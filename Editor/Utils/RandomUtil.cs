using System.Collections.Generic;

namespace Obfuz.Utils
{
    static class RandomUtil
    {
        public static void ShuffleList<T>(List<T> list, IRandom random)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.NextInt(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
