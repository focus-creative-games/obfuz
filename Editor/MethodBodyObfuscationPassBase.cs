using dnlib.DotNet.Emit;
using dnlib.DotNet;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz
{
    public abstract class MethodBodyObfuscationPassBase : ObfuscationPassBase
    {
        protected abstract bool NeedObfuscateMethod(MethodDef method);

        public override void Process(ObfuscatorContext ctx)
        {
            foreach (var ass in ctx.assemblies)
            {
                // ToArray to avoid modify list exception
                foreach (TypeDef type in ass.module.GetTypes().ToArray())
                {
                    if (type.Name.StartsWith("$Obfuz$"))
                    {
                        continue;
                    }
                    // ToArray to avoid modify list exception
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || method.Name.StartsWith("$Obfuz$") || !NeedObfuscateMethod(method))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }


        protected abstract bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions);

        private void ObfuscateData(MethodDef method)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                totalFinalInstructions.Add(inst);
                if (TryObfuscateInstruction(method, inst, instructions, i, outputInstructions))
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = outputInstructions[0].OpCode;
                    inst.Operand = outputInstructions[0].Operand;
                    for (int k = 1; k < outputInstructions.Count; k++)
                    {
                        totalFinalInstructions.Add(outputInstructions[k]);
                    }
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
