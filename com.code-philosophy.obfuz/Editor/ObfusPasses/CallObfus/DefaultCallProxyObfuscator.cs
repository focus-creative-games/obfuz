using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class DefaultCallProxyObfuscator : ObfuscatorBase
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly CallProxyAllocator _proxyCallAllocator;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public DefaultCallProxyObfuscator(EncryptionScopeProvider encryptionScopeProvider, ConstFieldAllocator constFieldAllocator, GroupByModuleEntityManager moduleEntityManager, CallObfuscationSettingsFacade settings)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _constFieldAllocator = constFieldAllocator;
            _moduleEntityManager = moduleEntityManager;
            _proxyCallAllocator = new CallProxyAllocator(encryptionScopeProvider, moduleEntityManager, settings);
        }

        public override void Done()
        {
            _proxyCallAllocator.Done();
        }

        public override void Obfuscate(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool needCacheCall, List<Instruction> obfuscatedInstructions)
        {

            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod));
            ProxyCallMethodData proxyCallMethodData = _proxyCallAllocator.Allocate(callerMethod.Module, calledMethod, callVir);
            DefaultMetadataImporter importer = _moduleEntityManager.GetDefaultModuleMetadataImporter(callerMethod.Module, _encryptionScopeProvider);

            if (needCacheCall)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(callerMethod.Module, proxyCallMethodData.index);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            }
            else
            {
                obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptedIndex));
                obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptOps));
                obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.salt));
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            }
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
        }
    }
}
