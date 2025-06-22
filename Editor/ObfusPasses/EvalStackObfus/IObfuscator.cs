using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.EvalStackObfus
{
    interface IObfuscator
    {
        bool ObfuscateInt(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);

        bool ObfuscateLong(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);

        bool ObfuscateFloat(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);

        bool ObfuscateDouble(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);
    }

    abstract class ObfuscatorBase : IObfuscator
    {
        public abstract bool ObfuscateInt(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);
        public abstract bool ObfuscateLong(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);
        public abstract bool ObfuscateFloat(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);
        public abstract bool ObfuscateDouble(Instruction inst, List<Instruction> outputInsts, ObfusMethodContext ctx);
    }
}
