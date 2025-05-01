using dnlib.DotNet;
using Obfuz.DynamicProxy;
using Obfuz.ExprObfuscation;
using Obfuz.MemEncrypt;
using Obfuz.Rename;
using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Obfuz.Virtualization;

namespace Obfuz
{


    public class Obfuscator
    {
        public class Options
        {
            public List<string> obfuscationAssemblyNames;
            public List<string> assemblySearchDirs;
            public List<string> obfuscationRuleFiles;
            public string mappingXmlPath;
            public string outputDir;
        }

        private readonly Options _options;
        private readonly AssemblyCache _assemblyCache;

        private readonly List<ObfuzAssemblyInfo> _obfuzAssemblies = new List<ObfuzAssemblyInfo>();


        private readonly List<string> _obfuscationAssemblyNames;

        public IList<string> ObfuscationAssemblyNames => _obfuscationAssemblyNames;

        private readonly ObfuzPipeline _pipeline = new ObfuzPipeline();

        private readonly ObfuscatorContext _ctx;

        public Obfuscator(Options options)
        {
            _options = options;
            _obfuscationAssemblyNames = options.obfuscationAssemblyNames;
            MetadataImporter.Reset();
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(options.assemblySearchDirs.ToArray()));

            _pipeline.AddPass(new MemoryEncryptionPass());
            //_pipeline.AddPass(new ProxyCallPass());
            //_pipeline.AddPass(new ExprObfuscationPass());
            //_pipeline.AddPass(new DataVirtualizationPass());
            _pipeline.AddPass(new RenameSymbolPass());
            _pipeline.AddPass(new CleanUpInstructionPass());


            _ctx = new ObfuscatorContext
            {
                assemblyCache = _assemblyCache,
                assemblies = _obfuzAssemblies,
                obfuscationAssemblyNames = _obfuscationAssemblyNames,
                obfuscationRuleFiles = options.obfuscationRuleFiles,
                mappingXmlPath = _options.mappingXmlPath,
                outputDir = options.outputDir,
            };

        }

        public void Run()
        {
            LoadAssemblies();
            _pipeline.Start(_ctx);
            DoObfuscation();
            OnObfuscationFinished();
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _obfuscationAssemblyNames)
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
                        UnityEngine.Debug.Log($"assembly:{ass.name} reference to {refAssName}");
                        refAssembly.referenceMeAssemblies.Add(ass);
                    }
                }
            }
        }

        private void DoObfuscation()
        {
            string outputDir = _options.outputDir;
            FileUtil.RecreateDir(outputDir);

            _pipeline.Run(_ctx);
        }

        private void OnObfuscationFinished()
        {
            string outputDir = _options.outputDir;

            _pipeline.Stop(_ctx);

            foreach (var ass in _obfuzAssemblies)
            {
                string outputFile = $"{outputDir}/{ass.module.Name}";
                ass.module.Write(outputFile);
                Debug.Log($"save module. oldName:{ass.name} newName:{ass.module.Name} output:{outputFile}");
            }
        }
    }
}
