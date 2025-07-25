using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Obfuz.Settings
{

    public class ObfuzSettings : ScriptableObject
    {
        [Tooltip("build pipeline settings")]
        public BuildPipelineSettings buildPipelineSettings;

        [Tooltip("assembly settings")]
        public AssemblySettings assemblySettings;

        [Tooltip("obfuscation pass settings")]
        public ObfuscationPassSettings obfuscationPassSettings;

        [Tooltip("secret settings")]
        public SecretSettings secretSettings;

        [Tooltip("encryption virtual machine settings")]
        public EncryptionVMSettings encryptionVMSettings;

        [Tooltip("symbol obfuscation settings")]
        public SymbolObfuscationSettings symbolObfusSettings;

        [Tooltip("const encryption settings")]
        public ConstEncryptionSettings constEncryptSettings;

        [Tooltip("eval stack obfuscation settings")]
        public EvalStackObfuscationSettings evalStackObfusSettings;

        [Tooltip("field encryption settings")]
        public FieldEncryptionSettings fieldEncryptSettings;

        [Tooltip("call obfuscation settings")]
        public CallObfuscationSettings callObfusSettings;

        [Tooltip("expression obfuscation settings")]
        public ExprObfuscationSettings exprObfusSettings;

        [Tooltip("control flow obfuscation settings")]
        public ControlFlowObfuscationSettings controlFlowObfusSettings;

        [Tooltip("garbage code generator settings")]
        public GarbageCodeGenerationSettings garbageCodeGenerationSettings;

        [Tooltip("polymorphic dll settings")]
        public PolymorphicDllSettings polymorphicDllSettings;

        public string ObfuzRootDir => $"Library/Obfuz";

        public string GetObfuscatedAssemblyOutputPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/ObfuscatedAssemblies";
        }

        public string GetOriginalAssemblyBackupDir(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/OriginalAssemblies";
        }

        public string GetObfuscatedAssemblyTempOutputPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/TempObfuscatedAssemblies";
        }

        public string GetObfuscatedLinkXmlPath(BuildTarget target)
        {
            return $"{ObfuzRootDir}/{target}/link.xml";
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

            if (arr != null && arr.Length > 0 && arr[0] is ObfuzSettings obfuzSettings)
            {
                s_Instance = obfuzSettings;
            }
            else
            {
                s_Instance = s_Instance ?? CreateInstance<ObfuzSettings>();
            }
            return s_Instance;
        }

        public static void Save()
        {
            if (!s_Instance)
            {
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
