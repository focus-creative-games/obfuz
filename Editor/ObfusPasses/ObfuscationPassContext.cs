using dnlib.DotNet;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses
{

    public class ObfuscationPassContext
    {
        public AssemblyCache assemblyCache;

        public List<ModuleDef> toObfuscatedModules;
        public List<ModuleDef> obfuscatedAndNotObfuscatedModules;

        public List<string> toObfuscatedAssemblyNames;
        public List<string> notObfuscatedAssemblyNamesReferencingObfuscated;

        public string obfuscatedAssemblyOutputDir;
    }
}
