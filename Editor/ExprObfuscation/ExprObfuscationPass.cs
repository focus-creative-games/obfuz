using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ExprObfuscation
{
    public class ExprObfuscationPass : MethodBodyObfuscationPassBase
    {


        public override void Start(ObfuscatorContext ctx)
        {

        }

        public override void Stop(ObfuscatorContext ctx)
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
