using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public class SymbolObfuscationSettingsFacade
    {
        public bool debug;
        public string obfuscatedNamePrefix;
        public bool useConsistentNamespaceObfuscation;
        public bool detectReflectionCompatibility;
        public bool keepUnknownSymbolInSymbolMappingFile;
        public string symbolMappingFile;
        public List<string> ruleFiles;
        public List<Type> customRenamePolicyTypes;
    }

    [Serializable]
    public class SymbolObfuscationSettings
    {
        public bool debug;

        [Tooltip("prefix for obfuscated name to avoid name confliction with original name")]
        public string obfuscatedNamePrefix = "$";

        [Tooltip("obfuscate same namespace to one name")]
        public bool useConsistentNamespaceObfuscation = true;

        [Tooltip("detect reflection compatibility, if true, will detect if the obfuscated name is compatibility with reflection, such as Type.GetType(), Enum.Parse(), etc.")]
        public bool detectReflectionCompatibility = true;

        [Tooltip("keep unknown symbol in symbol mapping file, if false, unknown symbol will be removed from mapping file")]
        public bool keepUnknownSymbolInSymbolMappingFile = true;

        [Tooltip("symbol mapping file path")]
        public string symbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping.xml";

        [Tooltip("debug symbol mapping file path, used for debugging purposes")]
        public string debugSymbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping-debug.xml";

        [Tooltip("rule files")]
        public string[] ruleFiles;

        [Tooltip("custom rename policy types")]
        public string[] customRenamePolicyTypes;

        public string GetSymbolMappingFile()
        {
            return debug ? debugSymbolMappingFile : symbolMappingFile;
        }

        public SymbolObfuscationSettingsFacade ToFacade()
        {
            return new SymbolObfuscationSettingsFacade
            {
                debug = debug,
                obfuscatedNamePrefix = obfuscatedNamePrefix,
                useConsistentNamespaceObfuscation = useConsistentNamespaceObfuscation,
                detectReflectionCompatibility = detectReflectionCompatibility,
                keepUnknownSymbolInSymbolMappingFile = keepUnknownSymbolInSymbolMappingFile,
                symbolMappingFile = GetSymbolMappingFile(),
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
                customRenamePolicyTypes = customRenamePolicyTypes?.Select(typeName => ReflectionUtil.FindUniqueTypeInCurrentAppDomain(typeName)).ToList() ?? new List<Type>(),
            };
        }
    }
}
