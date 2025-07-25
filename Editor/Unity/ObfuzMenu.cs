using Obfuz.EncryptionVM;
using Obfuz.GarbageCodeGeneration;
using Obfuz.Settings;
using Obfuz.Utils;
using System.IO;
using UnityEditor;
using UnityEngine;
using FileUtil = Obfuz.Utils.FileUtil;

namespace Obfuz.Unity
{
    public static class ObfuzMenu
    {

        [MenuItem("Obfuz/Settings...", priority = 1)]
        public static void OpenSettings() => SettingsService.OpenProjectSettings("Project/Obfuz");

        [MenuItem("Obfuz/GenerateEncryptionVM", priority = 62)]
        public static void GenerateEncryptionVM()
        {
            EncryptionVMSettings settings = ObfuzSettings.Instance.encryptionVMSettings;
            var generator = new VirtualMachineCodeGenerator(settings.codeGenerationSecretKey, settings.encryptionOpCodeCount);
            generator.Generate(settings.codeOutputPath);
            AssetDatabase.Refresh();
        }

        [MenuItem("Obfuz/GenerateSecretKeyFile", priority = 63)]
        public static void SaveSecretFile()
        {
            SecretSettings settings = ObfuzSettings.Instance.secretSettings;

            var staticSecretBytes = KeyGenerator.GenerateKey(settings.defaultStaticSecretKey, VirtualMachine.SecretKeyLength);
            SaveKey(staticSecretBytes, settings.staticSecretKeyOutputPath);
            Debug.Log($"Save static secret key to {settings.staticSecretKeyOutputPath}");
            var dynamicSecretBytes = KeyGenerator.GenerateKey(settings.defaultDynamicSecretKey, VirtualMachine.SecretKeyLength);
            SaveKey(dynamicSecretBytes, settings.dynamicSecretKeyOutputPath);
            Debug.Log($"Save dynamic secret key to {settings.dynamicSecretKeyOutputPath}");
            AssetDatabase.Refresh();
        }

        [MenuItem("Obfuz/GarbageCode/GenerateCodes", priority = 100)]
        public static void GenerateGarbageCodes()
        {
            Debug.Log($"Generating garbage codes begin.");
            GarbageCodeGenerationSettings settings = ObfuzSettings.Instance.garbageCodeGenerationSettings;
            var generator = new GarbageCodeGenerator(settings);
            generator.Generate();
            AssetDatabase.Refresh();
            Debug.Log($"Generating garbage codes end.");
        }

        [MenuItem("Obfuz/GarbageCode/CleanGeneratedCodes", priority = 101)]
        public static void CleanGeneratedGarbageCodes()
        {
            Debug.Log($"Clean generated garbage codes begin.");
            GarbageCodeGenerationSettings settings = ObfuzSettings.Instance.garbageCodeGenerationSettings;
            var generator = new GarbageCodeGenerator(settings);
            generator.CleanCodes();
            AssetDatabase.Refresh();
            Debug.Log($"Clean generated garbage codes end.");
        }

        private static void SaveKey(byte[] secret, string secretOutputPath)
        {
            FileUtil.CreateParentDir(secretOutputPath);
            File.WriteAllBytes(secretOutputPath, secret);
        }

        [MenuItem("Obfuz/Documents/Quick Start")]
        public static void OpenQuickStart() => Application.OpenURL("https://www.obfuz.com/docs/beginner/quickstart");

        [MenuItem("Obfuz/Documents/FAQ")]
        public static void OpenFAQ() => Application.OpenURL("https://www.obfuz.com/docs/help/faq");

        [MenuItem("Obfuz/Documents/Common Errors")]
        public static void OpenCommonErrors() => Application.OpenURL("https://www.obfuz.com/docs/help/commonerrors");

        [MenuItem("Obfuz/Documents/Bug Report")]
        public static void OpenBugReport() => Application.OpenURL("https://www.obfuz.com/docs/help/issue");

        [MenuItem("Obfuz/Documents/GitHub")]
        public static void OpenGitHub() => Application.OpenURL("https://github.com/focus-creative-games/obfuz");

        [MenuItem("Obfuz/Documents/About")]
        public static void OpenAbout() => Application.OpenURL("https://www.obfuz.com/docs/intro");
    }

}