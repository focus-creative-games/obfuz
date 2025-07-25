using System;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class PolymorphicDllSettings
    {
        [Tooltip("enable polymorphic DLL generation")]
        public bool enable = true;

        [Tooltip("secret key for generating polymorphic DLL source code")]
        public string codeGenerationSecretKey = "obfuz-polymorphic-key";

        [Tooltip("disable load standard dotnet dll")]
        public bool disableLoadStandardDll = false;
    }
}
