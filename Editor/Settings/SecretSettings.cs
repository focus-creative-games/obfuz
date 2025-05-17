using System;
using System.IO;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SecretSettings
    {

        [Tooltip("default static secret key")]
        public string defaultStaticSecretKey = "Code Philosophy-Static";

        [Tooltip("default dynamic secret key")]
        public string defaultDynamicSecretKey = "Code Philosophy-Dynamic";

        [Tooltip("secret key output path")]
        public string secretKeyOutputPath = $"Assets/Resources/Obfuz";

        [Tooltip("random seed")]
        public int randomSeed = 0;

        [Tooltip("name of assemblies those use dynamic secret key")]
        public string[] assembliesUsingDynamicSecretKeys;

        public string DefaultStaticSecretKeyOutputPath => Path.Combine(secretKeyOutputPath, "defaultStaticSecret.bytes");

        public string DefaultDynamicSecretKeyOutputPath => Path.Combine(secretKeyOutputPath, "defaultDynamicSecret.bytes");
    }
}
