using dnlib.DotNet;
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
        public List<ObfuzAssemblyInfo> assemblies;
    }
}
