using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{

    public class EvalStackObfuscationSettingsFacade
    {
        public List<string> ruleFiles;
    }

    [Serializable]
    public class EvalStackObfuscationSettings
    {
        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public EvalStackObfuscationSettingsFacade ToFacade()
        {
            return new EvalStackObfuscationSettingsFacade
            {
                ruleFiles = new List<string>(ruleFiles ?? Array.Empty<string>()),
            };
        }
    }
}
