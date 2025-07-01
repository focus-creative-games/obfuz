using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{

    public class DispatchProxyObfuscator : ObfuscatorBase
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly DispatchProxyAllocator _proxyCallAllocator;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public DispatchProxyObfuscator(EncryptionScopeProvider encryptionScopeProvider, ConstFieldAllocator constFieldAllocator, GroupByModuleEntityManager moduleEntityManager, CallObfuscationSettingsFacade settings)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _constFieldAllocator = constFieldAllocator;
            _moduleEntityManager = moduleEntityManager;
            _proxyCallAllocator = new DispatchProxyAllocator(encryptionScopeProvider, moduleEntityManager, settings);
        }

        public override void Done()
        {
            _proxyCallAllocator.Done();
        }

        public override bool Obfuscate(MethodDef callerMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {

            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod, null));
            ProxyCallMethodData proxyCallMethodData = _proxyCallAllocator.Allocate(callerMethod.Module, calledMethod, callVir);
            DefaultMetadataImporter importer = _moduleEntityManager.GetDefaultModuleMetadataImporter(callerMethod.Module, _encryptionScopeProvider);

            //if (needCacheCall)
            //{
            //    FieldDef cacheField = _constFieldAllocator.Allocate(callerMethod.Module, proxyCallMethodData.index);
            //    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            //}
            //else
            //{
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptedIndex));
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptOps));
            //    obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.salt));
            //    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            //}

            FieldDef cacheField = _constFieldAllocator.Allocate(callerMethod.Module, proxyCallMethodData.index);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
            return true;
        }
    }
}
