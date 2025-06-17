using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ExprObfus
{
    interface IObfuscator
    {
        bool ObfuscateBasicUnaryOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);

        bool ObfuscateBasicBinOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);

        bool ObfuscateUnaryBitwiseOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);

        bool ObfuscateBinBitwiseOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);

        bool ObfuscateBitShiftOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
    }

    abstract class ObfuscatorBase : IObfuscator
    {
        public abstract bool ObfuscateBasicUnaryOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
        public abstract bool ObfuscateBasicBinOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
        public abstract bool ObfuscateUnaryBitwiseOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
        public abstract bool ObfuscateBinBitwiseOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
        public abstract bool ObfuscateBitShiftOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts);
    }
}
