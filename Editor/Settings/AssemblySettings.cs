using System;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class AssemblySettings
    {

        [Tooltip("name of assemblies to obfuscated")]
        public string[] toObfuscatedAssemblyNames;

        [Tooltip("name of assemblies not obfuscated but reference assemblies to obfuscated ")]
        public string[] notObfuscatedAssemblyNamesReferencingObfuscated;

        [Tooltip("extra assembly search dirs")]
        public string[] extraAssemblySearchDirs;

        public string[] GetObfuscationRelativeAssemblyNames()
        {
            return toObfuscatedAssemblyNames
                .Concat(notObfuscatedAssemblyNamesReferencingObfuscated)
                .ToArray();
        }
    }
}
