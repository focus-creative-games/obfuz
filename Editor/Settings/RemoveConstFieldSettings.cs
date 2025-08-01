using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.Settings
{
    public class RemoveConstFieldSettingsFacade
    {
        public List<string> ruleFiles;
    }

    [Serializable]
    public class RemoveConstFieldSettings
    {
        [Tooltip("rule config xml files")]
        public string[] ruleFiles;

        public RemoveConstFieldSettingsFacade ToFacade()
        {
            return new RemoveConstFieldSettingsFacade
            {
                ruleFiles = ruleFiles?.ToList() ?? new List<string>(),
            };
        }
    }
}
