using dnlib.DotNet;
using Obfuz.Rename;
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
        public class Options
        {
            public List<string> AssemblySearchDirs;
            public List<string> ObfuscatedAssemblyNames;
            public string outputDir;
        }

        private readonly Options _options;
        private readonly AssemblyCache _assemblyCache;

        private readonly List<ObfuzAssemblyInfo> _obfuzAssemblies = new List<ObfuzAssemblyInfo>();

        private readonly IRenamePolicy _renamePolicy;
        private readonly INameMaker _nameMaker;

        public Obfuscator(Options options)
        {
            _options = options;
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(options.AssemblySearchDirs.ToArray()));
            _renamePolicy = new RenamePolicy();
            _nameMaker = new NameMaker();
        }

        public void DoIt()
        {
            LoadAssemblies();
            Rename();
            Save();
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _options.ObfuscatedAssemblyNames)
            {
                ModuleDefMD mod = _assemblyCache.LoadModule(assName);
                var obfuzAsm = new ObfuzAssemblyInfo
                {
                    name = assName,
                    module = mod,
                    referenceMeAssemblies = new List<ObfuzAssemblyInfo>(),
                };
                obfuzAsm.referenceMeAssemblies.Add(obfuzAsm);
                _obfuzAssemblies.Add(obfuzAsm);
            }
            
            var assByName = _obfuzAssemblies.ToDictionary(x => x.name);
            foreach (var ass in _obfuzAssemblies)
            {
                foreach (var refAss in ass.module.GetAssemblyRefs())
                {
                    string refAssName = refAss.Name.ToString();
                    if (assByName.TryGetValue(refAssName, out var refAssembly))
                    {
                        UnityEngine.Debug.Log($"assembly:{ass.name} reference to {refAssName}");
                        refAssembly.referenceMeAssemblies.Add(ass);
                    }
                }
            }
        }

        private void Rename()
        {
            var ctx = new ObfuscatorContext
            {
                assemblies = _obfuzAssemblies,
                renamePolicy = _renamePolicy,
                nameMaker = _nameMaker,
            };
            var sr = new SymbolRename(ctx);
            sr.Process();
        }

        private void Save()
        {
            string outputDir = _options.outputDir;
            FileUtil.RecreateDir(outputDir);
            foreach (var ass in _obfuzAssemblies)
            {
                string outputFile = $"{outputDir}/{ass.module.Name}";
                ass.module.Write(outputFile);
                Debug.Log($"save module. oldName:{ass.name} newName:{ass.module.Name} output:{outputFile}");
            }
        }
    }
}
