using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public enum ProxyMode
    {
        Dispatch,
        Delegate,
    }

    public class CallObfuscationSettingsFacade
    {
        public ProxyMode proxyMode;
        public int obfuscationLevel;
        public int maxProxyMethodCountPerDispatchMethod;
        public bool obfuscateCallToMethodInMscorlib;
        public List<string> ruleFiles;
    }

    [Serializable]
    public class CallObfuscationSettings
    {
        public ProxyMode proxyMode = ProxyMode.Dispatch;

        [Tooltip("The obfuscation level for the obfuscation. Higher levels provide more security but may impact performance.")]
        [Range(1, 4)]
        public int obfuscationLevel = 1;

        [Tooltip("The maximum number of proxy methods that can be generated per dispatch method. This helps to limit the complexity of the generated code and improve performance.")]
        public int maxProxyMethodCountPerDispatchMethod = 100;

        [Tooltip("Whether to obfuscate calls to methods in mscorlib. Enable this option will impact performance.")]
        public bool obfuscateCallToMethodInMscorlib;

        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public CallObfuscationSettingsFacade ToFacade()
        {
            return new CallObfuscationSettingsFacade
            {
                proxyMode = proxyMode,
                obfuscationLevel = obfuscationLevel,
                maxProxyMethodCountPerDispatchMethod = maxProxyMethodCountPerDispatchMethod,
                obfuscateCallToMethodInMscorlib = obfuscateCallToMethodInMscorlib,
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
            };
        }
    }
}
