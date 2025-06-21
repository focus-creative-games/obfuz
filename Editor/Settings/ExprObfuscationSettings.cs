using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{

    public class ExprObfuscationSettingsFacade
    {
        public List<string> ruleFiles;
    }

    [Serializable]
    public class ExprObfuscationSettings
    {
        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public ExprObfuscationSettingsFacade ToFacade()
        {
            return new ExprObfuscationSettingsFacade
            {
                ruleFiles = new List<string>(ruleFiles ?? Array.Empty<string>()),
            };
        }
    }
}
