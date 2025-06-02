using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    public interface IObfuscator
    {
        void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, bool needCacheCall, List<Instruction> obfuscatedInstructions);

        void Done();
    }

    public abstract class ObfuscatorBase : IObfuscator
    {
        public abstract void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, bool needCacheCall, List<Instruction> obfuscatedInstructions);
        public abstract void Done();
    }
}
