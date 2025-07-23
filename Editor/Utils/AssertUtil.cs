using UnityEngine.Assertions;

namespace Obfuz.Utils
{
    public static class AssertUtil
    {
        private static bool IsArrayEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static void AreArrayEqual(byte[] expected, byte[] actual, string message)
        {
            Assert.IsTrue(IsArrayEqual(expected, actual), message);
        }
    }
}
