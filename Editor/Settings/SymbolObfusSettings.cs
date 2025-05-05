using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class SymbolObfusSettings
    {
        public bool debug;

        [Tooltip("path of mapping.xml")]
        public string mappingFile = "Assets/Obfuz/SymbolObfus/mapping.xml";

        [Tooltip("obfuscation rule files for assemblies")]
        public string[] ruleFiles;
    }
}
