using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CallObfus;
using Obfuz.ObfusPasses.CleanUp;
using Obfuz.ObfusPasses.ConstEncrypt;
using Obfuz.ObfusPasses.ExprObfus;
using Obfuz.ObfusPasses.MemEncrypt;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Obfuz
{
    public class ObfuscatorBuilder
    {
        private List<string> _toObfuscatedAssemblyNames = new List<string>();
        private List<string> _notObfuscatedAssemblyNamesReferencingObfuscated = new List<string>();
        private List<string> _assemblySearchDirs = new List<string>();

        private string _obfuscatedAssemblyOutputDir;
        private List<IObfuscationPass> _obfuscationPasses = new List<IObfuscationPass>();

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
                _obfuscationPasses);
        }

        public static ObfuscatorBuilder FromObfuzSettings(ObfuzSettings settings, BuildTarget target)
        {
            var builder = new ObfuscatorBuilder
            {
                _toObfuscatedAssemblyNames = settings.toObfuscatedAssemblyNames.ToList(),
                _notObfuscatedAssemblyNamesReferencingObfuscated = settings.notObfuscatedAssemblyNamesReferencingObfuscated.ToList(),
                _assemblySearchDirs = settings.extraAssemblySearchDirs.ToList(),
                _obfuscatedAssemblyOutputDir = settings.GetObfuscatedAssemblyOutputDir(target),
            };
            ObfuscationPassType obfuscationPasses = settings.enabledObfuscationPasses;
            if (obfuscationPasses.HasFlag(ObfuscationPassType.MemoryEncryption))
            {
                builder.AddPass(new MemEncryptPass());
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.CallProxy))
            {
                builder.AddPass(new CallObfusPass(settings.callObfusSettings));
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.ConstEncryption))
            {
                builder.AddPass(new ConstEncryptPass(settings.constEncryptSettings));
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.ExprObfuscation))
            {
                builder.AddPass(new ExprObfusPass());
            }
            if (obfuscationPasses.HasFlag(ObfuscationPassType.SymbolObfuscation))
            {
                builder.AddPass(new SymbolObfusPass(settings.symbolObfusSettings));
            }
            return builder;
        }
    }
}
