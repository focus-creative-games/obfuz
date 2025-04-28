using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using Obfuz.Utils;
using Obfuz.Emit;

namespace Obfuz.DynamicProxy
{
    public class DefaultDynamicProxyObfuscator : DynamicProxyObfuscatorBase
    {
        private readonly IRandom _random;
        private readonly ProxyCallAllocator _proxyCallAllocator;

        public DefaultDynamicProxyObfuscator(IRandom random)
        {
            _random = random;
            _proxyCallAllocator = new ProxyCallAllocator(random);
        }

        public override void Done()
        {
            _proxyCallAllocator.Done();
        }

        public override void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            MethodSig sharedMethodSig = MetaUtil.ToSharedMethodSig(calledMethod.Module.CorLibTypes, MetaUtil.GetInflatedMethodSig(calledMethod));
            ProxyCallMethodData proxyCallMethodData = _proxyCallAllocator.Allocate(callingMethod.Module, calledMethod, callVir);
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(proxyCallMethodData.secret));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, proxyCallMethodData.proxyMethod));
        }
    }
}
