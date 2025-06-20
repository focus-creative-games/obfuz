using dnlib.DotNet.Emit;
using dnlib.DotNet;
using Obfuz.Emit;
using System.Collections.Generic;
using Obfuz.Utils;
using Obfuz.Data;
using UnityEngine;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class AdvancedObfuscator : BasicObfuscator
    {

        private void LoadConstInt(int a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        private void LoadConstLong(long a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_I8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        private void LoadConstFloat(float a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R4, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        private void LoadConstDouble(double a, IRandom random, float constProbability, ModuleConstFieldAllocator constFieldAllocator, List<Instruction> outputInsts)
        {
            Instruction inst;
            if (random.NextInPercentage(constProbability))
            {
                inst = Instruction.Create(OpCodes.Ldc_R8, a);
            }
            else
            {
                FieldDef field = constFieldAllocator.Allocate(a);
                inst = Instruction.Create(OpCodes.Ldsfld, field);
            }
            outputInsts.Add(inst);
        }

        public override bool ObfuscateBasicUnaryOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            DefaultMetadataImporter importer = ctx.importer;
            EncryptionScopeInfo encryptionScope = ctx.encryptionScope;
            IRandom random = ctx.localRandom;
            ModuleConstFieldAllocator constFieldAllocator = ctx.constFieldAllocator;

            switch (inst.OpCode.Code)
            {
                case Code.Neg:
                {
                    switch (op)
                    {
                        case EvalDataType.Int32:
                        {
                            // y = -x = (x * a + b) * (-ra) + b * ra;
                            int a = random.NextInt() | 0x1;
                            int ra = MathUtil.ModInverse32(a);
                            int b = random.NextInt();
                            int b_ra = b * ra;
                            float constProbability = 0.5f;
                            LoadConstInt(a, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            LoadConstInt(b, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            LoadConstInt(-ra, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            LoadConstInt(b_ra, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            return true;
                        }
                        case EvalDataType.Int64:
                        {
                            // y = -x = (x * a + b) * (-ra) + b * ra;
                            long a = random.NextLong() | 0x1L;
                            long ra = MathUtil.ModInverse64(a);
                            long b = random.NextLong();
                            long b_ra = b * ra;
                            float constProbability = 0.5f;
                            LoadConstLong(a, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            LoadConstLong(b, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            LoadConstLong(-ra, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            LoadConstLong(b_ra, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            return true;
                        }
                        case EvalDataType.Float:
                        {
                            // y = -x = (x + a) * b; a = 0.0f, b = 1.0f,
                            float a = 0.0f;
                            float b = -1.0f;
                            float constProbability = 0f;
                            LoadConstFloat(a, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            LoadConstFloat(b, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            return true;
                        }
                        case EvalDataType.Double:
                        {
                            // y = -x = (x + a) * b; a = 0.0, b = -1.0,
                            double a = 0.0;
                            double b = -1.0;
                            float constProbability = 0f;
                            LoadConstDouble(a, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Add));
                            LoadConstDouble(b, random, constProbability, constFieldAllocator, outputInsts);
                            outputInsts.Add(Instruction.Create(OpCodes.Mul));
                            return true;
                        }
                    }
                    return true;
                }
                default: return false;
            }
        }

        public override bool ObfuscateBasicBinOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (op1 != op2)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            
            return base.ObfuscateBasicBinOp(inst, op1, op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateUnaryBitwiseOp(Instruction inst, EvalDataType op, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            return base.ObfuscateUnaryBitwiseOp(inst, op, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBinBitwiseOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (op1 != op2)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            return base.ObfuscateBinBitwiseOp(inst, op1 , op2, ret, outputInsts, ctx);
        }

        public override bool ObfuscateBitShiftOp(Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, List<Instruction> outputInsts, ObfusMethodContext ctx)
        {
            if (op2 != EvalDataType.Int32)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with operand type {op2}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            return base.ObfuscateBitShiftOp(inst, op1, op2 , ret, outputInsts, ctx);
        }
    }
}
