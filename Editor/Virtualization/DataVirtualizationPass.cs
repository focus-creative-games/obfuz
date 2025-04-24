using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Virtualization
{

    public class DataVirtualizationPass : MethodBodyObfuscationPassBase
    {
        private IDataObfuscationPolicy _dataObfuscatorPolicy;
        private IDataObfuscator _dataObfuscator;

        public override void Start(ObfuscatorContext ctx)
        {
            _dataObfuscatorPolicy = new ConfigDataObfuscationPolicy();
            _dataObfuscator = new DefaultDataObfuscator();
        }

        public override void Stop(ObfuscatorContext ctx)
        {
            _dataObfuscator.Stop();
        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dataObfuscatorPolicy.NeedObfuscateMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef method, Instruction inst, IList<Instruction> instructions, int instructionIndex,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            switch (inst.OpCode.OperandType)
            {
                case OperandType.InlineI:
                case OperandType.InlineI8:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineR:
                case OperandType.InlineR:
                {
                    object operand = inst.Operand;
                    if (operand is int)
                    {
                        int value = (int)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is sbyte)
                    {
                        int value = (sbyte)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is byte)
                    {
                        int value = (byte)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is long)
                    {
                        long value = (long)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateLong(method, value))
                        {
                            _dataObfuscator.ObfuscateLong(method, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is float)
                    {
                        float value = (float)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateFloat(method, value))
                        {
                            _dataObfuscator.ObfuscateFloat(method, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is double)
                    {
                        double value = (double)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateDouble(method, value))
                        {
                            _dataObfuscator.ObfuscateDouble(method, value, outputInstructions);
                            return true;
                        }
                    }
                    return false;
                }
                case OperandType.InlineString:
                {
                    //RuntimeHelpers.InitializeArray
                    string value = (string)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateString(method, value))
                    {
                        _dataObfuscator.ObfuscateString(method, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case OperandType.InlineMethod:
                {
                    if (((IMethod)inst.Operand).FullName == "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)")
                    {
                        Instruction prevInst = instructions[instructionIndex - 1];
                        if (prevInst.OpCode.Code == Code.Ldtoken)
                        {
                            IField rvaField = (IField)prevInst.Operand;
                            FieldDef ravFieldDef = rvaField.ResolveFieldDefThrow();
                            byte[] data = ravFieldDef.InitialValue;
                            if (data != null && _dataObfuscatorPolicy.NeedObfuscateArray(method, data))
                            {
                                // remove prev ldtoken instruction
                                Assert.AreEqual(Code.Ldtoken, totalFinalInstructions[totalFinalInstructions.Count - 1].OpCode.Code);
                                totalFinalInstructions.RemoveAt(totalFinalInstructions.Count - 1);
                                _dataObfuscator.ObfuscateBytes(method, data, outputInstructions);
                                return true;
                            }
                        }
                    }
                    return false;
                }
                default: return false;
            }
        }
    }
}
