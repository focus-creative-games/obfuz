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
        private BuildTarget _buildTarget;

        private string _defaultStaticSecretKey;
        private string _defaultStaticSecretKeyOutputPath;
        private string _defaultDynamicSecretKey;
        private string _defaultDynamicSecretKeyOutputPath;
        private List<string> _assembliesUsingDynamicSecretKeys = new List<string>();

        private int _randomSeed;
        private string _encryptionVmGenerationSecretKey;
        private int _encryptionVmOpCodeCount;
        private string _encryptionVmCodeFile;

        private List<string> _assembliesToObfuscate = new List<string>();
        private List<string> _nonObfuscatedButReferencingObfuscatedAssemblies = new List<string>();
        private List<string> _assemblySearchPaths = new List<string>();

        private string _obfuscatedAssemblyTempOutputPath;
        private string _obfuscatedAssemblyOutputPath;
        private List<string> _obfuscationPassRuleConfigFiles;

        private ObfuscationPassType _enabledObfuscationPasses;
        private List<IObfuscationPass> _obfuscationPasses = new List<IObfuscationPass>();

        public BuildTarget BuildTarget
        {
            get => _buildTarget;
            set => _buildTarget = value;
        }

        public string DefaultStaticSecretKey
        {
            get => _defaultStaticSecretKey;
            set => _defaultStaticSecretKey = value;
        }

        public string DefaultStaticSecretKeyOutputPath
        {
            get => _defaultStaticSecretKeyOutputPath;
            set => _defaultStaticSecretKeyOutputPath = value;
        }

        public string DefaultDynamicSecretKey
        {
            get => _defaultDynamicSecretKey;
            set => _defaultDynamicSecretKey = value;
        }

        public string DefaultDynamicSecretKeyOutputPath
        {
            get => _defaultDynamicSecretKeyOutputPath;
            set => _defaultDynamicSecretKeyOutputPath = value;
        }

        public List<string> AssembliesUsingDynamicSecretKeys
        {
            get => _assembliesUsingDynamicSecretKeys;
            set => _assembliesUsingDynamicSecretKeys = value;
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

        public List<string> AssembliesToObfuscate
        {
            get => _assembliesToObfuscate;
            set => _assembliesToObfuscate = value;
        }

        public List<string> NonObfuscatedButReferencingObfuscatedAssemblies
        {
            get => _nonObfuscatedButReferencingObfuscatedAssemblies;
            set => _nonObfuscatedButReferencingObfuscatedAssemblies = value;
        }

        public List<string> AssemblySearchPaths
        {
            get => _assemblySearchPaths;
            set => _assemblySearchPaths = value;
        }

        public string ObfuscatedAssemblyOutputPath
        {
            get => _obfuscatedAssemblyOutputPath;
            set => _obfuscatedAssemblyOutputPath = value;
        }

        public string ObfuscatedAssemblyTempOutputPath
        {
            get => _obfuscatedAssemblyTempOutputPath;
            set => _obfuscatedAssemblyTempOutputPath = value;
        }

        public ObfuscationPassType EnableObfuscationPasses
        {
            get => _enabledObfuscationPasses;
            set => _enabledObfuscationPasses = value;
        }

        public List<string> ObfuscationPassRuleConfigFiles
        {
            get => _obfuscationPassRuleConfigFiles;
            set => _obfuscationPassRuleConfigFiles = value;
        }

        public List<IObfuscationPass> ObfuscationPasses
        {
            get => _obfuscationPasses;
            set => _obfuscationPasses = value;
        }

        public void InsertTopPriorityAssemblySearchPaths(List<string> assemblySearchPaths)
        {
            _assemblySearchPaths.InsertRange(0, assemblySearchPaths);
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
            var searchPaths = new List<string>
                {
#if UNITY_2021_1_OR_NEWER
#if UNITY_STANDALONE_WIN || (UNITY_EDITOR_WIN && UNITY_SERVER) || UNITY_WSA || UNITY_LUMIN
                "MonoBleedingEdge/lib/mono/unityaot-win32",
                "MonoBleedingEdge/lib/mono/unityaot-win32/Facades",
#elif UNITY_STANDALONE_OSX || (UNITY_EDITOR_OSX && UNITY_SERVER) || UNITY_IOS || UNITY_TVOS
                "MonoBleedingEdge/lib/mono/unityaot-macos",
                "MonoBleedingEdge/lib/mono/unityaot-macos/Facades",
#else
                "MonoBleedingEdge/lib/mono/unityaot-linux",
                "MonoBleedingEdge/lib/mono/unityaot-linux/Facades",
#endif
#else
                "MonoBleedingEdge/lib/mono/unityaot",
                "MonoBleedingEdge/lib/mono/unityaot/Facades",
#endif

#if UNITY_STANDALONE_WIN || (UNITY_EDITOR_WIN && UNITY_SERVER)
                "PlaybackEngines\\windowsstandalonesupport\\Variations\\il2cpp\\Managed",
#elif UNITY_STANDALONE_OSX || (UNITY_EDITOR_OSX && UNITY_SERVER)
                "PlaybackEngines\\MacStandaloneSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_STANDALONE_LINUX || (UNITY_EDITOR_LINUX && UNITY_SERVER)
                "PlaybackEngines\\LinuxStandaloneSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_ANDROID
                "PlaybackEngines\\AndroidPlayer\\Variations\\il2cpp\\Managed",
#elif UNITY_IOS
                "PlaybackEngines\\iOSSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_WEBGL
                "PlaybackEngines\\WebGLSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_MINIGAME || UNITY_WEIXINMINIGAME
                "PlaybackEngines\\WeixinMiniGameSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_OPENHARMONY
                "PlaybackEngines\\OpenHarmonyPlayer\\Variations\\il2cpp\\Managed",
#elif UNITY_TVOS
                "PlaybackEngines\AppleTVSupport\\Variations\\il2cpp\\Managed",
#elif UNITY_WSA
                "PlaybackEngines\\WSASupport\\Variations\\il2cpp\\Managed",
#elif UNITY_LUMIN
                "PlaybackEngines\\LuminSupport\\Variations\\il2cpp\\Managed",
#else
#error "Unsupported platform, please report to us"
#endif

                "Managed/UnityEngine",
                };
            return searchPaths.Select(path => Path.Combine(applicationContentsPath, path)).ToList();
        }

        public static ObfuscatorBuilder FromObfuzSettings(ObfuzSettings settings, BuildTarget target, bool searchPathIncludeUnityEditorInstallLocation)
        {
            List<string> searchPaths = searchPathIncludeUnityEditorInstallLocation ?
                BuildUnityAssemblySearchPaths().Concat(settings.assemblySettings.additionalAssemblySearchPaths).ToList()
                : settings.assemblySettings.additionalAssemblySearchPaths.ToList();
            var builder = new ObfuscatorBuilder
            {
                _buildTarget = target,
                _defaultStaticSecretKey = settings.secretSettings.defaultStaticSecretKey,
                _defaultStaticSecretKeyOutputPath = settings.secretSettings.DefaultStaticSecretKeyOutputPath,
                _defaultDynamicSecretKey = settings.secretSettings.defaultDynamicSecretKey,
                _defaultDynamicSecretKeyOutputPath = settings.secretSettings.DefaultDynamicSecretKeyOutputPath,
                _assembliesUsingDynamicSecretKeys = settings.secretSettings.assembliesUsingDynamicSecretKeys.ToList(),
                _randomSeed = settings.secretSettings.randomSeed,
                _encryptionVmGenerationSecretKey = settings.encryptionVMSettings.codeGenerationSecretKey,
                _encryptionVmOpCodeCount = settings.encryptionVMSettings.encryptionOpCodeCount,
                _encryptionVmCodeFile = settings.encryptionVMSettings.codeOutputPath,
                _assembliesToObfuscate = settings.assemblySettings.GetAssembliesToObfuscate(),
                _nonObfuscatedButReferencingObfuscatedAssemblies = settings.assemblySettings.nonObfuscatedButReferencingObfuscatedAssemblies.ToList(),
                _assemblySearchPaths = searchPaths,
                _obfuscatedAssemblyOutputPath = settings.GetObfuscatedAssemblyOutputPath(target),
                _obfuscatedAssemblyTempOutputPath = settings.GetObfuscatedAssemblyTempOutputPath(target),
                _enabledObfuscationPasses = settings.obfuscationPassSettings.enabledPasses,
                _obfuscationPassRuleConfigFiles = settings.obfuscationPassSettings.ruleFiles.ToList(),
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
