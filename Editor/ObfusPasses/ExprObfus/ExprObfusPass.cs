using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.ExprObfus
{
    public class ExprObfusPass : InstructionObfuscationPassBase
    {


        public override void Start(ObfuscationPassContext ctx)
        {

        }

        public override void Stop(ObfuscationPassContext ctx)
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return false;
        }

        protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            return false;
        }
    }
}
