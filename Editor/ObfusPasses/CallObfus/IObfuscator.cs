using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    public interface IObfuscator
    {
        bool Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions);

        void Done();
    }

    public abstract class ObfuscatorBase : IObfuscator
    {
        public abstract bool Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions);

        public abstract void Done();
    }
}
