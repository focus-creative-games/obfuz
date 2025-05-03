using Obfuz.DynamicProxy;
using Obfuz.ExprObfuscation;
using Obfuz.MemEncrypt;
using Obfuz.Rename;
using System.Collections.Generic;
using System.Linq;
using Obfuz.Virtualization;
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
            builder.AddPass(new MemoryEncryptionPass());
            builder.AddPass(new ProxyCallPass());
            builder.AddPass(new ExprObfuscationPass());
            builder.AddPass(new DataVirtualizationPass());
            builder.AddPass(new RenameSymbolPass(settings.ruleFiles.ToList(), settings.mappingFile));
            return builder;
        }
    }
}
