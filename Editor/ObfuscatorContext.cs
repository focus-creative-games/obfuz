using dnlib.DotNet;
using Obfuz.Rename;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{

    public class ObfuscatorContext
    {
        public AssemblyCache assemblyCache;

        public List<ModuleDef> toObfuscatedModules;
        public List<ModuleDef> obfuscatedAndNotObfuscatedModules;

        public List<string> toObfuscatedAssemblyNames;
        public List<string> notObfuscatedAssemblyNamesReferencingObfuscated;

        public string obfuscatedAssemblyOutputDir;
    }
}
