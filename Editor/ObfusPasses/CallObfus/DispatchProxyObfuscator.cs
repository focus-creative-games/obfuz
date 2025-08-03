using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{

    public class DispatchProxyObfuscator : ObfuscatorBase
    {
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public DispatchProxyObfuscator(GroupByModuleEntityManager moduleEntityManager)
        {
            _moduleEntityManager = moduleEntityManager;
        }

        public override void Done()
        {
            _moduleEntityManager.Done<ModuleDispatchProxyAllocator>();
        }

        public override bool Obfuscate(MethodDef callerMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            ModuleDispatchProxyAllocator proxyCallAllocator = _moduleEntityManager.GetEntity<ModuleDispatchProxyAllocator>(callerMethod.Module);
            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod, null));
            ProxyCallMethodData proxyCallMethodData = proxyCallAllocator.Allocate(calledMethod, callVir);
            DefaultMetadataImporter importer = proxyCallAllocator.GetDefaultModuleMetadataImporter();

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

            ConstFieldAllocator constFieldAllocator = proxyCallAllocator.GetEntity<ConstFieldAllocator>();
            FieldDef cacheField = constFieldAllocator.Allocate(proxyCallMethodData.index);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
            return true;
        }
    }
}
