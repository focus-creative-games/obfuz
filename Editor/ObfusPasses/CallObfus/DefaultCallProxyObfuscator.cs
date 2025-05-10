using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using Obfuz.Utils;
using Obfuz.Emit;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class DefaultCallProxyObfuscator : ObfuscatorBase
    {
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly CallProxyAllocator _proxyCallAllocator;

        public DefaultCallProxyObfuscator(IRandom random, IEncryptor encryptor)
        {
            _random = random;
            _encryptor = encryptor;
            _proxyCallAllocator = new CallProxyAllocator(random, _encryptor);
        }

        public override void Done()
        {
            _proxyCallAllocator.Done();
        }

        public override void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod));
            ProxyCallMethodData proxyCallMethodData = _proxyCallAllocator.Allocate(callingMethod.Module, calledMethod, callVir);
            DefaultModuleMetadataImporter importer = MetadataImporter.Instance.GetDefaultModuleMetadataImporter(callingMethod.Module);
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptedIndex));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.encryptOps));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
        }
    }
}
