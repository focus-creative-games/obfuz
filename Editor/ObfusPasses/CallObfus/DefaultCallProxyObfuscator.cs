using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using Obfuz.Utils;
using Obfuz.Emit;
using Obfuz.Data;
using UnityEngine;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class DefaultCallProxyObfuscator : ObfuscatorBase
    {
        private readonly IEncryptor _encryptor;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly CallProxyAllocator _proxyCallAllocator;

        public DefaultCallProxyObfuscator(IRandom random, IEncryptor encryptor, ConstFieldAllocator constFieldAllocator, int encryptionLevel)
        {
            _encryptor = encryptor;
            _constFieldAllocator = constFieldAllocator;
            _proxyCallAllocator = new CallProxyAllocator(random, _encryptor, encryptionLevel);
        }

        public override void Done()
        {
            _proxyCallAllocator.Done();
        }

        public override void Obfuscate(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool needCacheCall, List<Instruction> obfuscatedInstructions)
        {

            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod));
            ProxyCallMethodData proxyCallMethodData = _proxyCallAllocator.Allocate(callerMethod.Module, calledMethod, callVir);
            DefaultMetadataImporter importer = GroupByModuleEntityManager.Ins.GetDefaultModuleMetadataImporter(callerMethod.Module);

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
