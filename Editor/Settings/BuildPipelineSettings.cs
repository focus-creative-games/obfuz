using System;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class BuildPipelineSettings
    {
        [Tooltip("enable Obfuz")]
        public bool enable = true;

        [Tooltip("callback order of LinkXmlProcessor")]
        public int linkXmlProcessCallbackOrder = 10000;

        [Tooltip("callback order of ObfuscationProcess")]
        public int obfuscationProcessCallbackOrder = 10000;
    }
}
