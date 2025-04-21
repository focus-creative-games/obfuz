using dnlib.DotNet;
using Obfuz.Rename;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class ObfuzAssemblyInfo
    {
        public string name;

        public ModuleDefMD module;

        public List<ObfuzAssemblyInfo> referenceMeAssemblies;
    }

    public class ObfuscatorContext
    {
        public AssemblyCache assemblyCache;

        public List<ObfuzAssemblyInfo> assemblies;

        public List<string> obfuscationAssemblyNames;
        public List<string> obfuscationRuleFiles;
        public string mappingXmlPath;

        public string outputDir;
    }
}
