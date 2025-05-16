using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.ConstEncrypt
{

    public class ConstEncryptPass : BasicBlockObfuscationPassBase
    {
        private readonly List<string> _configFiles;
        private readonly int _encryptionLevel;
        private IEncryptPolicy _dataObfuscatorPolicy;
        private IConstEncryptor _dataObfuscator;
        public override ObfuscationPassType Type => ObfuscationPassType.ConstEncrypt;

        public ConstEncryptPass(ConstEncryptSettings settings)
        {
            _configFiles = settings.configFiles.ToList();
            _encryptionLevel = settings.encryptionLevel;
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _dataObfuscatorPolicy = new ConfigurableEncryptPolicy(ctx.toObfuscatedAssemblyNames, _configFiles);
            _dataObfuscator = new DefaultConstEncryptor(ctx.encryptionScopeProvider, ctx.rvaDataAllocator, ctx.constFieldAllocator, ctx.moduleEntityManager, _encryptionLevel);
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dataObfuscatorPolicy.NeedObfuscateMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef method, Instruction inst, BasicBlock block, int instructionIndex, IList<Instruction> globalInstructions,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            bool currentInLoop = block.inLoop;
            ConstCachePolicy constCachePolicy = _dataObfuscatorPolicy.GetMethodConstCachePolicy(method);
            switch (inst.OpCode.OperandType)
            {
                case OperandType.InlineI:
                case OperandType.InlineI8:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineR:
                case OperandType.InlineR:
                {
                    bool needCache = currentInLoop ? constCachePolicy.cacheConstInLoop : constCachePolicy.cacheConstNotInLoop;
                    object operand = inst.Operand;
                    if (operand is int)
                    {
                        int value = (int)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is sbyte)
                    {
                        int value = (sbyte)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is byte)
                    {
                        int value = (byte)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateInt(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateInt(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is long)
                    {
                        long value = (long)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateLong(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateLong(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is float)
                    {
                        float value = (float)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateFloat(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateFloat(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    else if (operand is double)
                    {
                        double value = (double)operand;
                        if (_dataObfuscatorPolicy.NeedObfuscateDouble(method, currentInLoop, value))
                        {
                            _dataObfuscator.ObfuscateDouble(method, needCache, value, outputInstructions);
                            return true;
                        }
                    }
                    return false;
                }
                case OperandType.InlineString:
                {
                    //RuntimeHelpers.InitializeArray
                    string value = (string)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateString(method, currentInLoop, value))
                    {
                        bool needCache = currentInLoop ? constCachePolicy.cacheStringInLoop : constCachePolicy.cacheStringNotInLoop;
                        _dataObfuscator.ObfuscateString(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case OperandType.InlineMethod:
                {
                    //if (((IMethod)inst.Operand).FullName == "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)")
                    //{
                    //    Instruction prevInst = globalInstructions[instructionIndex - 1];
                    //    if (prevInst.OpCode.Code == Code.Ldtoken)
                    //    {
                    //        IField rvaField = (IField)prevInst.Operand;
                    //        FieldDef ravFieldDef = rvaField.ResolveFieldDefThrow();
                    //        byte[] data = ravFieldDef.InitialValue;
                    //        if (data != null && _dataObfuscatorPolicy.NeedObfuscateArray(method, currentInLoop, data))
                    //        {
                    //            if (_encryptedRvaFields.Add(ravFieldDef))
                    //            {
                                    
                    //            }

                    //            // remove prev ldtoken instruction
                    //            Assert.AreEqual(Code.Ldtoken, totalFinalInstructions.Last().OpCode.Code);
                    //            //totalFinalInstructions.RemoveAt(totalFinalInstructions.Count - 1);
                    //            // dup arr argument for decryption operation
                    //            totalFinalInstructions.Insert(totalFinalInstructions.Count - 1, Instruction.Create(OpCodes.Dup));
                    //            totalFinalInstructions.Add(inst.Clone());
                    //            //bool needCache = currentInLoop ? constCachePolicy.cacheStringInLoop : constCachePolicy.cacheStringNotInLoop;
                    //            bool needCache = false;
                    //            _dataObfuscator.ObfuscateBytes(method, needCache, data, outputInstructions);
                    //            return true;
                    //        }
                    //    }
                    //}
                    return false;
                }
                default: return false;
            }
        }
    }
}
