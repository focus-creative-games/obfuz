using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.Settings
{
    [Serializable]
    public class EncryptionVMSettings
    {
        [Tooltip("secret key for generating encryption virtual machine source code")]
        public string codeGenerationSecretKey = "Obfuz";

        [Tooltip("encryption virtual machine source code output dir")]
        public string codeOutputDir = "Assets/Obfuz";
    }
}
