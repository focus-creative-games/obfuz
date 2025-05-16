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
    public delegate IRandom RandomCreator(int seed);

    public class EncryptionScopeInfo
    {
        public readonly byte[] byteSecret;
        public readonly int[] intSecret;
        public readonly IEncryptor encryptor;
        public readonly RandomCreator localRandomCreator;

        public EncryptionScopeInfo(byte[] byteSecret, int[] intSecret, IEncryptor encryptor, RandomCreator localRandomCreator)
        {
            this.byteSecret = byteSecret;
            this.intSecret = intSecret;
            this.encryptor = encryptor;
            this.localRandomCreator = localRandomCreator;
        }
    }

    public class EncryptionScopeProvider
    {
        private readonly EncryptionScopeInfo _defaultStaticScope;
        private readonly EncryptionScopeInfo _defaultDynamicScope;
        private readonly HashSet<string> _dynamicSecretAssemblyNames;

        public EncryptionScopeProvider(EncryptionScopeInfo defaultStaticScope, EncryptionScopeInfo defaultDynamicScope, HashSet<string> dynamicSecretAssemblyNames)
        {
            _defaultStaticScope = defaultStaticScope;
            _defaultDynamicScope = defaultDynamicScope;
            _dynamicSecretAssemblyNames = dynamicSecretAssemblyNames;
        }

        public EncryptionScopeInfo GetScope(ModuleDef module)
        {
            if (_dynamicSecretAssemblyNames.Contains(module.Assembly.Name))
            {
                return _defaultDynamicScope;
            }
            else
            {
                return _defaultStaticScope;
            }
        }

        public bool IsDynamicSecretAssembly(ModuleDef module)
        {
            return _dynamicSecretAssemblyNames.Contains(module.Assembly.Name);
        }
    }

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

        public EncryptionScopeProvider encryptionScopeProvider;
        public ConstFieldAllocator constFieldAllocator;
        public RvaDataAllocator rvaDataAllocator;
        public NotObfuscatedMethodWhiteList whiteList;
        public ConfigurablePassPolicy passPolicy;
    }
}
