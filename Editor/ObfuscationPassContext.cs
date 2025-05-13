using dnlib.DotNet;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.SymbolObfus;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{

    public class ObfuscationPassContext
    {
        public static ObfuscationPassContext Current { get; set; }


        public GroupByModuleEntityManager moduleEntityManager;

        public AssemblyCache assemblyCache;

        public List<ModuleDef> toObfuscatedModules;
        public List<ModuleDef> obfuscatedAndNotObfuscatedModules;

        public List<string> toObfuscatedAssemblyNames;
        public List<string> notObfuscatedAssemblyNamesReferencingObfuscated;

        public string obfuscatedAssemblyOutputDir;

        public IRandom globalRandom;
        public Func<int, IRandom> localScopeRandomCreator;
        
        public IEncryptor encryptor;
        public ConstFieldAllocator constFieldAllocator;
        public RvaDataAllocator rvaDataAllocator;
        public NotObfuscatedMethodWhiteList whiteList;
        public ConfigurablePassPolicy passPolicy;
    }
}
