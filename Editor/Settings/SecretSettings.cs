using System;
using System.IO;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SecretSettings
    {

        [Tooltip("default static secret key")]
        public string defaultStaticSecret = "Code Philosophy-Static";

        public string defaultDynamicSecret = "Code Philosophy-Dynamic";

        [Tooltip("secret key output directory")]
        public string secretOutputDir = $"Assets/Resources/Obfuz";

        [Tooltip("random seed")]
        public int randomSeed = 0;

        [Tooltip("name of assemblies those use dynamic secret")]
        public string[] dynamicSecretAssemblyNames;

        public string DefaultStaticSecretKeyOutputPath => Path.Combine(secretOutputDir, "defaultStaticSecret.bytes");

        public string DefaultDynamicSecretKeyOutputPath => Path.Combine(secretOutputDir, "defaultDynamicSecret.bytes");
    }
}
