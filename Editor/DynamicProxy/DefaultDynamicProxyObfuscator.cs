using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;

namespace Obfuz.DynamicProxy
{
    public class DefaultDynamicProxyObfuscator : DynamicProxyObfuscatorBase
    {
        public override void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            // Default implementation does nothing
        }
    }
}
