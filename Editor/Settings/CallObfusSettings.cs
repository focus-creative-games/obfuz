using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class CallObfusSettings
    {
        [Tooltip("The obfuscation level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int callObfuscationLevel = 1;

        [Tooltip("config xml files")]
        public string[] configFiles;
    }
}
