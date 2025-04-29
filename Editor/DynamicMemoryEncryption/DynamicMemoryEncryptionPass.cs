using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMemoryEncryptionPass : MethodBodyObfuscationPassBase
{
    public override void Start(ObfuscatorContext ctx)
    {

    }

    public override void Stop(ObfuscatorContext ctx)
    {

    }

    protected override bool NeedObfuscateMethod(MethodDef method)
    {
        return true;
    }

    protected override bool TryObfuscateInstruction(MethodDef callingMethod, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
    {
        throw new System.NotImplementedException();
    }
}
