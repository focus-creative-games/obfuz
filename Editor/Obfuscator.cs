using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{


    public class Obfuscator
    {
        public class Options
        {
            public List<string> AssemblySearchDirs;
            public List<string> ObfusAssemblyNames;
            public string outputDir;
        }

        private readonly Options _options;
        private readonly AssemblyCache _assemblyCache;

        private readonly List<ObfuzAssemblyInfo> _obfuzAssemblies = new List<ObfuzAssemblyInfo>();

        public Obfuscator(Options options)
        {
            _options = options;
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(options.AssemblySearchDirs.ToArray()));
        }

        public void DoIt()
        {
            LoadAssemblies();
            Rename();
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _options.ObfusAssemblyNames)
            {
                ModuleDefMD mod = _assemblyCache.LoadModule(assName);
                var obfuzAsm = new ObfuzAssemblyInfo
                {
                    name = assName,
                    module = mod,
                    referenceMeAssemblies = new List<ObfuzAssemblyInfo>(),
                };
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
            };
            var sr = new SymbolRename(ctx);
            sr.Process();
        }
    }
}
