using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{

    class BasicObfuscator : ObfuscatorBase
    {
        private readonly EncryptionScopeProvider _encryptionScopeProvider;
        private readonly GroupByModuleEntityManager _moduleEntityManager;

        public BasicObfuscator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager)
        {
            _encryptionScopeProvider = encryptionScopeProvider;
            _moduleEntityManager = moduleEntityManager;
        }

        private DefaultMetadataImporter GetModuleMetadataImporter(ModuleDef module)
        {
            return _moduleEntityManager.GetDefaultModuleMetadataImporter(module, _encryptionScopeProvider);
        }

        private IMethod GetMethod(DefaultMetadataImporter importer, Code code, EvalDataType op1)
        {
            switch (code)
            {
                case Code.Add:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.AddInt;
                        case EvalDataType.Int64: return importer.AddLong;
                        case EvalDataType.Float: return importer.AddFloat;
                        case EvalDataType.Double: return importer.AddDouble;
                        default: return null;
                    }
                }
                case Code.Sub:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.SubtractInt;
                        case EvalDataType.Int64: return importer.SubtractLong;
                        case EvalDataType.Float: return importer.SubtractFloat;
                        case EvalDataType.Double: return importer.SubtractDouble;
                        default: return null;
                    }
                }
                case Code.Mul:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.MultiplyInt;
                        case EvalDataType.Int64: return importer.MultiplyLong;
                        case EvalDataType.Float: return importer.MultiplyFloat;
                        case EvalDataType.Double: return importer.MultiplyDouble;
                        default: return null;
                    }
                }
                case Code.Div:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.DivideInt;
                        case EvalDataType.Int64: return importer.DivideLong;
                        case EvalDataType.Float: return importer.DivideFloat;
                        case EvalDataType.Double: return importer.DivideDouble;
                        default: return null;
                    }
                }
                case Code.Div_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.DivideUnInt;
                        case EvalDataType.Int64: return importer.DivideUnLong;
                        default: return null;
                    }
                }
                case Code.Rem:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.RemInt;
                        case EvalDataType.Int64: return importer.RemLong;
                        case EvalDataType.Float: return importer.RemFloat;
                        case EvalDataType.Double: return importer.RemDouble;
                        default: return null;
                    }
                }
                case Code.Rem_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.RemUnInt;
                        case EvalDataType.Int64: return importer.RemUnLong;
                        default: return null;
                    }
                }
                case Code.Neg:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NegInt;
                        case EvalDataType.Int64: return importer.NegLong;
                        case EvalDataType.Float: return importer.NegFloat;
                        case EvalDataType.Double: return importer.NegDouble;
                        default: return null;
                    }
                }
                case Code.And:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.AndInt;
                        case EvalDataType.Int64: return importer.AndLong;
                        default: return null;
                    }
                }
                case Code.Or:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.OrInt;
                        case EvalDataType.Int64: return importer.OrLong;
                        default: return null;
                    }
                }
                case Code.Xor:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.XorInt;
                        case EvalDataType.Int64: return importer.XorLong;
                        default: return null;
                    }
                }
                case Code.Not:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.NotInt;
                        case EvalDataType.Int64: return importer.NotLong;
                        default: return null;
                    }
                }
                case Code.Shl:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShlInt;
                        case EvalDataType.Int64: return importer.ShlLong;
                        default: return null;
                    }
                }
                case Code.Shr:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShrInt;
                        case EvalDataType.Int64: return importer.ShrLong;
                        default: return null;
                    }
                }
                case Code.Shr_Un:
                {
                    switch (op1)
                    {
                        case EvalDataType.Int32: return importer.ShrUnInt;
                        case EvalDataType.Int64: return importer.ShrUnLong;
                        default: return null;
                    }
                }
                default: return null;
            }
        }

        public override bool ObfuscateBasicUnaryOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method.Module);
            IMethod opMethod = GetMethod(importer, inst.OpCode.Code, op);
            if (opMethod == null)
            {
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBasicBinOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            if (op1 != op2)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method.Module);
            IMethod opMethod = GetMethod(importer, inst.OpCode.Code, op1);
            if (opMethod == null)
            {
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateUnaryBitwiseOp(MethodDef method, Instruction inst, EvalDataType op, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method.Module);
            IMethod opMethod = GetMethod(importer, inst.OpCode.Code, op);
            if (opMethod == null)
            {
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBinBitwiseOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            if (op1 != op2)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with different operand types: op1={op1}, op2={op2}, ret={ret}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method.Module);
            IMethod opMethod = GetMethod(importer, inst.OpCode.Code, op1);
            if (opMethod == null)
            {
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }

        public override bool ObfuscateBitShiftOp(MethodDef method, Instruction inst, EvalDataType op1, EvalDataType op2, EvalDataType ret, LocalVariableAllocator localVariableAllocator, List<Instruction> outputInsts)
        {
            if (op2 != EvalDataType.Int32)
            {
                Debug.LogWarning($"BasicObfuscator: Cannot obfuscate binary operation {inst.OpCode.Code} with operand type {op2}. This is a limitation of the BasicObfuscator.");
                return false;
            }
            DefaultMetadataImporter importer = GetModuleMetadataImporter(method.Module);
            IMethod opMethod = GetMethod(importer, inst.OpCode.Code, op1);
            if (opMethod == null)
            {
                return false;
            }
            outputInsts.Add(Instruction.Create(OpCodes.Call, opMethod));
            return true;
        }
    }
}
