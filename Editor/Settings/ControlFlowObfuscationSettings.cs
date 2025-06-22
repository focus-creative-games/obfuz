using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{

    public class ControlFlowObfuscationSettingsFacade
    {
        public int minInstructionCountOfBasicBlockToObfuscate;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class ControlFlowObfuscationSettings
    {
        public int minInstructionCountOfBasicBlockToObfuscate = 3;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public ControlFlowObfuscationSettingsFacade ToFacade()
        {
            return new ControlFlowObfuscationSettingsFacade
            {
                minInstructionCountOfBasicBlockToObfuscate = minInstructionCountOfBasicBlockToObfuscate,
                ruleFiles = new List<string>(ruleFiles ?? Array.Empty<string>()),
            };
        }
    }
}
