using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;

namespace Obfuz.DynamicProxy
{
    public abstract class DynamicProxyObfuscatorBase : IDynamicProxyObfuscator
    {
        public abstract void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions);
        public abstract void Done();
    }
}
