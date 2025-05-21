using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SymbolObfuscationSettings
    {
        public bool debug;

        [Tooltip("prefix for obfuscated name to avoid name confliction with original name")]
        public string obfuscatedNamePrefix = "$";

        [Tooltip("obfuscate same namespace to one name")]
        public bool useConsistentNamespaceObfuscation = true;

        [Tooltip("symbol mapping file path")]
        public string symbolMappingFile = "Assets/Obfuz/SymbolObfus/symbol-mapping.xml";

        [Tooltip("rule files")]
        public string[] ruleFiles;
    }
}
