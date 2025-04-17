using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Obfuz
{

    public class ObfuzSettings : ScriptableObject
    {
        [Tooltip("enable Obfuz")]
        public bool enable = true;

        [Tooltip("obfuscated assembly names(without .dll suffix)")]
        public string[] obfuscatedAssemblyNames;

        public string ObfuzRootDir => $"Library/Obfuz";

        public string GetMappingFile(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/mapping.xml";
        }

        public string GetObfuscatedAssemblyOutputDir(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/ObfuscatedAssemblies";
        }

        public string GetOriginalAssemblyBackupDir(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/OriginalAssemblies";
        }

        private static ObfuzSettings s_Instance;

        public static ObfuzSettings Instance
        {
            get
            {
                if (!s_Instance)
                {
                    LoadOrCreate();
                }
                return s_Instance;
            }
        }

        protected static string SettingsPath => "ProjectSettings/Obfuz.asset";

        private static ObfuzSettings LoadOrCreate()
        {
            string filePath = SettingsPath;
            var arr = InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            //Debug.Log($"typeof arr:{arr?.GetType()} arr[0]:{(arr != null && arr.Length > 0 ? arr[0].GetType(): null)}");

            s_Instance = arr != null && arr.Length > 0 ? (ObfuzSettings)arr[0] : CreateInstance<ObfuzSettings>();
            return s_Instance;
        }

        public static void Save()
        {
            if (!s_Instance)
            {
                Debug.LogError("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string filePath = SettingsPath;
            string directoryName = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directoryName);
            UnityEngine.Object[] obj = new ObfuzSettings[1] { s_Instance };
            InternalEditorUtility.SaveToSerializedFileAndForget(obj, filePath, true);
        }
    }
}
