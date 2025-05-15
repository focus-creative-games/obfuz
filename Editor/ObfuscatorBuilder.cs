using Obfuz.EncryptionVM;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CallObfus;
using Obfuz.ObfusPasses.ConstEncrypt;
using Obfuz.ObfusPasses.ExprObfus;
using Obfuz.ObfusPasses.FieldEncrypt;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Settings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Obfuz
{
    public class ObfuscatorBuilder
    {
        private string _secret;
        private string _secretOutputPath;
        private int _randomSeed;
        private string _encryptionVmGenerationSecretKey;
        private int _encryptionVmOpCodeCount;
        private string _encryptionVmCodeFile;

        private List<string> _toObfuscatedAssemblyNames = new List<string>();
        private List<string> _notObfuscatedAssemblyNamesReferencingObfuscated = new List<string>();
        private List<string> _assemblySearchDirs = new List<string>();

        private string _obfuscatedAssemblyOutputDir;
        private List<string> _obfuscationPassConfigFiles;

        private ObfuscationPassType _enabledObfuscationPasses;
        private List<IObfuscationPass> _obfuscationPasses = new List<IObfuscationPass>();

        public string Secret
        {
            get => _secret;
            set => _secret = value;
        }

        public string SecretOutputPath
        {
            get => _secretOutputPath;
            set => _secretOutputPath = value;
        }

        public int RandomSeed
        {
            get => _randomSeed;
            set => _randomSeed = value;
        }

        public string EncryptionVmGenerationSecretKey
        {
            get => _encryptionVmGenerationSecretKey;
            set => _encryptionVmGenerationSecretKey = value;
        }

        public int EncryptionVmOpCodeCount
        {
            get => _encryptionVmOpCodeCount;
            set => _encryptionVmOpCodeCount = value;
        }

        public string EncryptionVmCodeFile
        {
            get => _encryptionVmCodeFile;
            set => _encryptionVmCodeFile = value;
        }

        public List<string> ToObfuscatedAssemblyNames
        {
            get => _toObfuscatedAssemblyNames;
            set => _toObfuscatedAssemblyNames = value;
        }

        public List<string> NotObfuscatedAssemblyNamesReferencingObfuscated
        {
            get => _notObfuscatedAssemblyNamesReferencingObfuscated;
            set => _notObfuscatedAssemblyNamesReferencingObfuscated = value;
        }

        public List<string> AssemblySearchDirs
        {
            get => _assemblySearchDirs;
            set => _assemblySearchDirs = value;
        }

        public string ObfuscatedAssemblyOutputDir
        {
            get => _obfuscatedAssemblyOutputDir;
            set => _obfuscatedAssemblyOutputDir = value;
        }

        public ObfuscationPassType EnableObfuscationPasses
        {
            get => _enabledObfuscationPasses;
            set => _enabledObfuscationPasses = value;
        }

        public List<string> ObfuscationPassConfigFiles
        {
            get => _obfuscationPassConfigFiles;
            set => _obfuscationPassConfigFiles = value;
        }

        public List<IObfuscationPass> ObfuscationPasses
        {
            get => _obfuscationPasses;
            set => _obfuscationPasses = value;
        }

        public void InsertTopPriorityAssemblySearchDirs(List<string> assemblySearchDirs)
        {
            _assemblySearchDirs.InsertRange(0, assemblySearchDirs);
        }

        public ObfuscatorBuilder AddPass(IObfuscationPass pass)
        {
            _obfuscationPasses.Add(pass);
            return this;
        }

        public Obfuscator Build()
        {
            return new Obfuscator(this);
        }

        public static List<string> BuildUnityAssemblySearchPaths()
        {
            string applicationContentsPath = EditorApplication.applicationContentsPath;
            return new List<string>
                {
#if UNITY_2021_1_OR_NEWER
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api/Facades"),
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api"),
#elif UNITY_2020 || UNITY_2019
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api/Facades"),
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api"),
#else
#error "Unsupported Unity version"
#endif
                    Path.Combine(applicationContentsPath, "Managed/UnityEngine"),
                };
        }

        public static ObfuscatorBuilder FromObfuzSettings(ObfuzSettings settings, BuildTarget target, bool searchPathIncludeUnityEditorInstallLocation)
        {
            List<string> searchPaths = searchPathIncludeUnityEditorInstallLocation ?
                BuildUnityAssemblySearchPaths().Concat(settings.assemblySettings.extraAssemblySearchDirs).ToList()
                : settings.assemblySettings.extraAssemblySearchDirs.ToList();
            var builder = new ObfuscatorBuilder
            {
                _secret = settings.secretSettings.secret,
                _secretOutputPath = settings.secretSettings.secretOutputPath,
                _randomSeed = settings.secretSettings.randomSeed,
                _encryptionVmGenerationSecretKey = settings.encryptionVMSettings.codeGenerationSecret,
                _encryptionVmOpCodeCount = settings.encryptionVMSettings.encryptionOpCodeCount,
                _encryptionVmCodeFile = settings.encryptionVMSettings.codeOutputPath,
                _toObfuscatedAssemblyNames = settings.assemblySettings.toObfuscatedAssemblyNames.ToList(),
                _notObfuscatedAssemblyNamesReferencingObfuscated = settings.assemblySettings.notObfuscatedAssemblyNamesReferencingObfuscated.ToList(),
                _assemblySearchDirs = searchPaths,
                _obfuscatedAssemblyOutputDir = settings.GetObfuscatedAssemblyOutputDir(target),
                _enabledObfuscationPasses = settings.obfuscationPassSettings.enabledPasses,
                _obfuscationPassConfigFiles = settings.obfuscationPassSettings.configFiles.ToList(),
            };
            ObfuscationPassType obfuscationPasses = settings.obfuscationPassSettings.enabledPasses;
            if (obfuscationPasses.HasFlag(ObfuscationPassType.ConstEncrypt))
            {
                builder.AddPass(new ConstEncryptPass(settings.constEncryptSettings));
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.FieldEncrypt))
            {
                builder.AddPass(new FieldEncryptPass(settings.fieldEncryptSettings));
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.CallObfus))
            {
                builder.AddPass(new CallObfusPass(settings.callObfusSettings));
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.ExprObfus))
            {
                builder.AddPass(new ExprObfusPass());
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.SymbolObfus))
            {
                builder.AddPass(new SymbolObfusPass(settings.symbolObfusSettings));
            }
            return builder;
        }
    }
}
