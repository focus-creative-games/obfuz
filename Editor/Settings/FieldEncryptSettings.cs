using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class FieldEncryptSettings
    {
        [Tooltip("config xml files")]
        public string[] configFiles;
    }
}
