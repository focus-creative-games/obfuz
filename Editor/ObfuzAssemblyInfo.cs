using dnlib.DotNet;
using System.Collections.Generic;

namespace Obfuz
{
    public class ObfuzAssemblyInfo
    {
        public string name;

        public ModuleDefMD module;

        public List<ObfuzAssemblyInfo> referenceMeAssemblies;
    }
}
