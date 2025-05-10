using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CallObfus
{
    public interface IObfuscator
    {
        void Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, bool needCacheCall, List<Instruction> obfuscatedInstructions);

        void Done();
    }
}
