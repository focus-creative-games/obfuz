using System;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SecretSettings
    {

        [Tooltip("secret key")]
        public string secret = "Code Philosophy";

        [Tooltip("secret key save path")]
        public string secretOutputPath = $"Assets/Obfuz/secret.bytes";

        [Tooltip("random seed")]
        public int randomSeed = 0;
    }
}
