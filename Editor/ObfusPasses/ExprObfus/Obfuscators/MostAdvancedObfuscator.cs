using dnlib.DotNet.Emit;
using Obfuz.Emit;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class MostAdvancedObfuscator : AdvancedObfuscator
    {
        private readonly BasicObfuscator _basicObfuscator = new BasicObfuscator();

        public override bool ObfuscateBasicUnaryOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBasicUnaryOp(inst, op, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBasicUnaryOp(inst, op, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBasicBinOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBasicBinOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBasicBinOp(inst, op1, op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateUnaryBitwiseOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateUnaryBitwiseOp(inst, op, ret, outputInsts, ctx))
            {
                return false;
            }

            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateUnaryBitwiseOp(inst, op, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBinBitwiseOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBinBitwiseOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBinBitwiseOp(inst, op1, op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBitShiftOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (!base.ObfuscateBitShiftOp(inst, op1, op2, ret, outputInsts, ctx))
            {
                return false;
            }
            if (outputInsts.Last().OpCode.Code != inst.OpCode.Code)
            {
                return false;
            }
            outputInsts.RemoveAt(outputInsts.Count - 1);
            return _basicObfuscator.ObfuscateBitShiftOp(inst, op1, op2, ret, outputInsts, ctx);
        }
    }
}
