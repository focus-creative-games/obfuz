using System;
using UnityEngine;

namespace Obfuz.Settings
{
    public class WatermarkSettingsFacade
    {
        public string text;

        public int signatureLength;
    }

    [Serializable]
    public class WatermarkSettings
    {
        [Tooltip("Watermark text")]
        public string text = "Obfuscated by Obfuz";

        [Tooltip("Length of the signature in bytes")]
        public int signatureLength = 256;

        public WatermarkSettingsFacade ToFacade()
        {
            return new WatermarkSettingsFacade
            {
                text = text,
                signatureLength = signatureLength
            };
        }
    }
}
