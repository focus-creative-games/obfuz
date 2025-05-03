using dnlib.DotNet;
using Obfuz.Emit;
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

        private readonly List<ObfuzAssemblyInfo> _obfuzAssemblies = new List<ObfuzAssemblyInfo>();


        private readonly List<string> _toObfuscatedAssemblyNames;
        private readonly List<string> _notObfuscatedAssemblyNamesReferencingObfuscated;

        private readonly ObfuzPipeline _pipeline = new ObfuzPipeline();

        private ObfuscatorContext _ctx;

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

            _pipeline.AddPass(new CleanUpInstructionPass());


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


            _ctx = new ObfuscatorContext
            {
                assemblyCache = _assemblyCache,
                assemblies = _obfuzAssemblies,
                toObfuscatedAssemblyNames = _toObfuscatedAssemblyNames,
                notObfuscatedAssemblyNamesReferencingObfuscated = _notObfuscatedAssemblyNamesReferencingObfuscated,
                obfuscatedAssemblyOutputDir = _obfuscatedAssemblyOutputDir,
            };
            _pipeline.Start(_ctx);
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _toObfuscatedAssemblyNames)
            {
                ModuleDefMD mod = _assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
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
                        //UnityEngine.Debug.Log($"assembly:{ass.name} reference to {refAssName}");
                        refAssembly.referenceMeAssemblies.Add(ass);
                    }
                }
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

            foreach (var ass in _obfuzAssemblies)
            {
                string outputFile = $"{_obfuscatedAssemblyOutputDir}/{ass.module.Name}";
                ass.module.Write(outputFile);
                Debug.Log($"save module. oldName:{ass.name} newName:{ass.module.Name} output:{outputFile}");
            }
        }
    }
}
