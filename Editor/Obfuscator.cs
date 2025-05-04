using dnlib.DotNet;
using Obfuz.Emit;
using Obfuz.ObfusPasses;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz
{

    public class Obfuscator
    {
        private readonly string _obfuscatedAssemblyOutputDir;
        private readonly AssemblyCache _assemblyCache;

        private readonly List<string> _toObfuscatedAssemblyNames;
        private readonly List<string> _notObfuscatedAssemblyNamesReferencingObfuscated;
        private readonly List<ModuleDef> _toObfuscatedModules = new List<ModuleDef>();
        private readonly List<ModuleDef> _obfuscatedAndNotObfuscatedModules = new List<ModuleDef>();

        private readonly Pipeline _pipeline = new Pipeline();

        private ObfuscationPassContext _ctx;

        public Obfuscator(List<string> toObfuscatedAssemblyNames,
            List<string> notObfuscatedAssemblyNamesReferencingObfuscated,
            List<string> assemblySearchDirs,
            string obfuscatedAssemblyOutputDir,
            List<IObfuscationPass> obfuscationPasses)
        {
            _toObfuscatedAssemblyNames = toObfuscatedAssemblyNames;
            _notObfuscatedAssemblyNamesReferencingObfuscated = notObfuscatedAssemblyNamesReferencingObfuscated;
            _obfuscatedAssemblyOutputDir = obfuscatedAssemblyOutputDir;

            MetadataImporter.Reset();
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(assemblySearchDirs.ToArray()));
            foreach (var pass in obfuscationPasses)
            {
                _pipeline.AddPass(pass);
            }
        }

        public void Run()
        {
            OnPreObfuscation();
            DoObfuscation();
            OnPostObfuscation();
        }

        private void OnPreObfuscation()
        {
            LoadAssemblies();

            _ctx = new ObfuscationPassContext
            {
                assemblyCache = _assemblyCache,
                toObfuscatedModules = _toObfuscatedModules,
                obfuscatedAndNotObfuscatedModules = _obfuscatedAndNotObfuscatedModules,
                toObfuscatedAssemblyNames = _toObfuscatedAssemblyNames,
                notObfuscatedAssemblyNamesReferencingObfuscated = _notObfuscatedAssemblyNamesReferencingObfuscated,
                obfuscatedAssemblyOutputDir = _obfuscatedAssemblyOutputDir,
            };
            _pipeline.Start(_ctx);
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _toObfuscatedAssemblyNames.Concat(_notObfuscatedAssemblyNamesReferencingObfuscated))
            {
                ModuleDefMD mod = _assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
                if (_toObfuscatedAssemblyNames.Contains(assName))
                {
                    _toObfuscatedModules.Add(mod);
                }
                _obfuscatedAndNotObfuscatedModules.Add(mod);
            }
        }

        private void DoObfuscation()
        {
            FileUtil.RecreateDir(_obfuscatedAssemblyOutputDir);

            _pipeline.Run(_ctx);
        }

        private void OnPostObfuscation()
        {
            _pipeline.Stop(_ctx);

            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                string assNameWithExt = mod.Name;
                string outputFile = $"{_obfuscatedAssemblyOutputDir}/{assNameWithExt}";
                mod.Write(outputFile);
                Debug.Log($"save module. name:{mod.Assembly.Name} output:{outputFile}");
            }
        }
    }
}
