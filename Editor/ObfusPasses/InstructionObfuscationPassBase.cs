using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses
{
    public abstract class InstructionObfuscationPassBase : ObfuscationMethodPassBase
    {
        protected abstract bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions);

        protected override void ObfuscateData(MethodDef method)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                outputInstructions.Clear();
                if (TryObfuscateInstruction(method, inst, instructions, i, outputInstructions, totalFinalInstructions))
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = outputInstructions[0].OpCode;
                    inst.Operand = outputInstructions[0].Operand;
                    totalFinalInstructions.Add(inst);
                    for (int k = 1; k < outputInstructions.Count; k++)
                    {
                        totalFinalInstructions.Add(outputInstructions[k]);
                    }
                }
                else
                {
                    totalFinalInstructions.Add(inst);
                }
            }

            instructions.Clear();
            foreach (var obInst in totalFinalInstructions)
            {
                instructions.Add(obInst);
            }
        }
    }
}
