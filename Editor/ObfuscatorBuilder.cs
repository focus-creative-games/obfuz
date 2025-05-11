using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CallObfus;
using Obfuz.ObfusPasses.ConstEncrypt;
using Obfuz.ObfusPasses.ExprObfus;
using Obfuz.ObfusPasses.FieldEncrypt;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Obfuz
{
    public class ObfuscatorBuilder
    {
        private string _secretKey;
        private int _globalRandomSeed;
        private string _encryptionVmGenerationSecretKey;
        private List<string> _toObfuscatedAssemblyNames = new List<string>();
        private List<string> _notObfuscatedAssemblyNamesReferencingObfuscated = new List<string>();
        private List<string> _assemblySearchDirs = new List<string>();

        private string _obfuscatedAssemblyOutputDir;
        private List<IObfuscationPass> _obfuscationPasses = new List<IObfuscationPass>();

        public string SecretKey
        {
            get => _secretKey;
            set => _secretKey = value;
        }

        public int GlobalRandomSeed
        {
            get => _globalRandomSeed;
            set => _globalRandomSeed = value;
        }

        public string EncryptionVmGenerationSecretKey
        {
            get => _encryptionVmGenerationSecretKey;
            set => _encryptionVmGenerationSecretKey = value;
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
            return new Obfuscator(_toObfuscatedAssemblyNames,
                _notObfuscatedAssemblyNamesReferencingObfuscated,
                _assemblySearchDirs,
                _obfuscatedAssemblyOutputDir,
                _obfuscationPasses, _secretKey, _globalRandomSeed, _encryptionVmGenerationSecretKey);
        }

        public static ObfuscatorBuilder FromObfuzSettings(ObfuzSettings settings, BuildTarget target)
        {
            var builder = new ObfuscatorBuilder
            {
                _secretKey = settings.secretKey,
                _globalRandomSeed = settings.globalRandomSeed,
                _encryptionVmGenerationSecretKey = settings.encryptionVMSettings.codeGenerationSecretKey,
                _toObfuscatedAssemblyNames = settings.toObfuscatedAssemblyNames.ToList(),
                _notObfuscatedAssemblyNamesReferencingObfuscated = settings.notObfuscatedAssemblyNamesReferencingObfuscated.ToList(),
                _assemblySearchDirs = settings.extraAssemblySearchDirs.ToList(),
                _obfuscatedAssemblyOutputDir = settings.GetObfuscatedAssemblyOutputDir(target),
            };
            ObfuscationPassType obfuscationPasses = settings.enabledObfuscationPasses;
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
