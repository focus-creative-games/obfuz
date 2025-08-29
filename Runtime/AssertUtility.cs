using System;

namespace Obfuz
{
    public static class AssertUtility
    {
        public static void VerifySecretKey(int expectedValue, int actualValue)
        {
            if (expectedValue != actualValue)
            {
                throw new Exception($"VerifySecretKey failed. Your secret key is unmatched with secret key used by current assembly in obfuscation");
            }
        }
    }
}
