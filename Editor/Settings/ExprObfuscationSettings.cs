using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obfuz.Settings
{
    public enum ObfuscationLevel
    {
        None = 0,
        Basic = 1,
        Advanced = 2,
        MostAdvanced = 3
    }

    public class ExprObfuscationSettingsFacade
    {
        public ObfuscationLevel obfuscationLevel;
        public float obfuscationPercentage;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class ExprObfuscationSettings
    {
        [Tooltip("Obfuscation level")]
        public ObfuscationLevel obfuscationLevel = ObfuscationLevel.Basic;

        [Tooltip("percentage of obfuscation, 0.0 - 1.0, 0.5 means 50% of expressions will be obfuscated")]
        [Range(0.1f, 1.0f)]
        public float obfuscationPercentage = 0.5f;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public ExprObfuscationSettingsFacade ToFacade()
        {
            return new ExprObfuscationSettingsFacade
            {
                obfuscationLevel = obfuscationLevel,
                obfuscationPercentage = obfuscationPercentage,
                ruleFiles = new List<string>(ruleFiles),
            };
        }
    }
}
