using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class NoneObfuscator : ObfuscatorBase
    {
        public override bool ObfuscateBasicUnaryOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            return false;
        }

        public override bool ObfuscateBasicBinOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            return false;
        }

        public override bool ObfuscateUnaryBitwiseOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            return false;
        }

        public override bool ObfuscateBinBitwiseOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            return false;
        }

        public override bool ObfuscateBitShiftOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            return false;
        }
    }
}
