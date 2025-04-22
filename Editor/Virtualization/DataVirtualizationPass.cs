using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Virtualization
{

    public class DataVirtualizationPass : ObfuscationPassBase
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

        }

        public override void Process(ObfuscatorContext ctx)
        {
            foreach (var ass in ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (!method.HasBody || !_dataObfuscatorPolicy.NeedObfuscateMethod(method))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }

        private void ObfuscateData(MethodDef method)
        {
            IList<Instruction> instructions = method.Body.Instructions;
            var obfuscatedInstructions = new List<Instruction>();
            var resultInstructions = new List<Instruction>();
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                bool obfuscated = false;
                switch (inst.OpCode.OperandType)
                {
                    case OperandType.InlineI:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineR:
                    case OperandType.InlineR:
                    {
                        obfuscatedInstructions.Clear();
                        object operand = inst.Operand;
                        if (operand is int)
                        {
                            int value = (int)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                            {
                                _dataObfuscator.ObfuscateInt(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        else if (operand is sbyte)
                        {
                            int value = (sbyte)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                            {
                                _dataObfuscator.ObfuscateInt(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        else if (operand is byte)
                        {
                            int value = (byte)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateInt(method, value))
                            {
                                _dataObfuscator.ObfuscateInt(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        else if (operand is long)
                        {
                            long value = (long)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateLong(method, value))
                            {
                                _dataObfuscator.ObfuscateLong(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        else if (operand is float)
                        {
                            float value = (float)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateFloat(method, value))
                            {
                                _dataObfuscator.ObfuscateFloat(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        else if (operand is double)
                        {
                            double value = (double)operand;
                            if (_dataObfuscatorPolicy.NeedObfuscateDouble(method, value))
                            {
                                _dataObfuscator.ObfuscateDouble(method, value, obfuscatedInstructions);
                                obfuscated = true;
                            }
                        }
                        break;
                    }
                    case OperandType.InlineString:
                    {
                        //RuntimeHelpers.InitializeArray
                        string value = (string)inst.Operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateString(method, value))
                        {
                            _dataObfuscator.ObfuscateString(method, value, obfuscatedInstructions);
                            obfuscated = true;
                        }
                        break;
                    }
                }
                resultInstructions.Add(inst);
                if (obfuscated)
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = OpCodes.Nop;
                    inst.Operand = null;
                    resultInstructions.AddRange(obfuscatedInstructions);
                }
            }

            instructions.Clear();
            foreach (var obInst in obfuscatedInstructions)
            {
                instructions.Add(obInst);
            }
        }
    }
}
