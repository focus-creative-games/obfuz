using System;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class AssemblySettings
    {

        [Tooltip("name of assemblies to obfuscate")]
        public string[] assembliesToObfuscate;

        [Tooltip("name of assemblies not obfuscated but reference assemblies to obfuscated ")]
        public string[] nonObfuscatedButReferencingObfuscatedAssemblies;

        [Tooltip("additional assembly search paths")]
        public string[] additionalAssemblySearchPaths;

        public string[] GetObfuscationRelativeAssemblyNames()
        {
            return assembliesToObfuscate
                .Concat(nonObfuscatedButReferencingObfuscatedAssemblies)
                .ToArray();
        }
    }
}
