using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public class CallObfuscationSettingsFacade
    {
        public List<string> ruleFiles;
        public int obfuscationLevel;
        public int maxProxyMethodCountPerDispatchMethod;
        public bool obfuscateCallToMethodInMscorlib;
    }

    [Serializable]
    public class CallObfuscationSettings
    {
        [Tooltip("The obfuscation level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int obfuscationLevel = 1;

        [Tooltip("The maximum number of proxy methods that can be generated per dispatch method. This helps to limit the complexity of the generated code and improve performance.")]
        public int maxProxyMethodCountPerDispatchMethod = 100;

        [Tooltip("Whether to obfuscate calls to methods in mscorlib. This can help to protect against reverse engineering, but may cause compatibility issues with some libraries.")]
        public bool obfuscateCallToMethodInMscorlib;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public CallObfuscationSettingsFacade ToFacade()
        {
            return new CallObfuscationSettingsFacade
            {
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
                obfuscationLevel = obfuscationLevel,
                maxProxyMethodCountPerDispatchMethod = maxProxyMethodCountPerDispatchMethod,
                obfuscateCallToMethodInMscorlib = obfuscateCallToMethodInMscorlib,
            };
        }
    }
}
