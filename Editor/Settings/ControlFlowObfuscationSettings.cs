using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{

    public class ControlFlowObfuscationSettingsFacade
    {
        public List<string> ruleFiles;
    }

    [Serializable]
    public class ControlFlowObfuscationSettings
    {
        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public ControlFlowObfuscationSettingsFacade ToFacade()
        {
            return new ControlFlowObfuscationSettingsFacade
            {
                ruleFiles = new List<string>(ruleFiles ?? Array.Empty<string>()),
            };
        }
    }
}
