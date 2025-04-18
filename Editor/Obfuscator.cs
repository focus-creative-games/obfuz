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
            public List<string> ObfuscationRuleFiles;
            public string mappingXmlPath;
            public string outputDir;
        }

        private readonly Options _options;
        private readonly AssemblyCache _assemblyCache;
        private readonly ObfuscateRuleConfig _obfuscateRuleConfig;

        private readonly List<ObfuzAssemblyInfo> _obfuzAssemblies = new List<ObfuzAssemblyInfo>();

        private readonly IRenamePolicy _renamePolicy;
        private readonly INameMaker _nameMaker;
        private SymbolRename _symbolRename;

        public IList<string> ObfuscatedAssemblyNames => _obfuzAssemblies.Select(x => x.name).ToList();

        public Obfuscator(Options options)
        {
            _options = options;
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(options.AssemblySearchDirs.ToArray()));
            _obfuscateRuleConfig = new ObfuscateRuleConfig();
            _obfuscateRuleConfig.LoadXmls(options.ObfuscationRuleFiles);
            _renamePolicy = new CacheRenamePolicy(new CombineRenamePolicy(new SystemRenamePolicy(), new UnityRenamePolicy(), _obfuscateRuleConfig));
            //_nameMaker = new TestNameMaker();
            _nameMaker = NameMakerFactory.CreateNameMakerBaseASCIICharSet();

        }

        public void Run()
        {
            LoadAssemblies();
            Rename();
            Save();
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _obfuscateRuleConfig.ObfuscatedAssemblyNames)
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

        private void Rename()
        {
            var ctx = new ObfuscatorContext
            {
                assemblyCache = _assemblyCache,
                assemblies = _obfuzAssemblies,
                renamePolicy = _renamePolicy,
                nameMaker = _nameMaker,
                mappingXmlPath = _options.mappingXmlPath,
                outputDir = _options.outputDir,
            };
            _symbolRename = new SymbolRename(ctx);
            _symbolRename.Process();
        }

        private void Save()
        {
            string outputDir = _options.outputDir;
            FileUtil.RecreateDir(outputDir);
            _symbolRename.Save();
            foreach (var ass in _obfuzAssemblies)
            {
                string outputFile = $"{outputDir}/{ass.module.Name}";
                ass.module.Write(outputFile);
                Debug.Log($"save module. oldName:{ass.name} newName:{ass.module.Name} output:{outputFile}");
            }
        }
    }
}
