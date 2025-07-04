﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Settings;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ConstEncrypt
{

    public class ConstEncryptPass : BasicBlockObfuscationPassBase
    {
        private readonly ConstEncryptionSettingsFacade _settings;
        private IEncryptPolicy _dataObfuscatorPolicy;
        private IConstEncryptor _dataObfuscator;
        public override ObfuscationPassType Type => ObfuscationPassType.ConstEncrypt;

        public ConstEncryptPass(ConstEncryptionSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _dataObfuscatorPolicy = new ConfigurableEncryptPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
            _dataObfuscator = new DefaultConstEncryptor(ctx.moduleEntityManager, _settings);
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
            bool needCache = currentInLoop ? constCachePolicy.cacheConstInLoop : constCachePolicy.cacheConstNotInLoop;
            switch (inst.OpCode.Code)
            {
                case Code.Ldc_I4:
                case Code.Ldc_I4_S:
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                case Code.Ldc_I4_M1:
                {
                    int value = inst.GetLdcI4Value();
                    if (_dataObfuscatorPolicy.NeedObfuscateInt(method, currentInLoop, value))
                    {
                        _dataObfuscator.ObfuscateInt(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case Code.Ldc_I8:
                {
                    long value = (long)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateLong(method, currentInLoop, value))
                    {
                        _dataObfuscator.ObfuscateLong(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case Code.Ldc_R4:
                {
                    float value = (float)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateFloat(method, currentInLoop, value))
                    {
                        _dataObfuscator.ObfuscateFloat(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case Code.Ldc_R8:
                {
                    double value = (double)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateDouble(method, currentInLoop, value))
                    {
                        _dataObfuscator.ObfuscateDouble(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case Code.Ldstr:
                {
                    string value = (string)inst.Operand;
                    if (_dataObfuscatorPolicy.NeedObfuscateString(method, currentInLoop, value))
                    {
                        _dataObfuscator.ObfuscateString(method, needCache, value, outputInstructions);
                        return true;
                    }
                    return false;
                }
                case Code.Call:
                {
                    if (((IMethod)inst.Operand).FullName == "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)")
                    {
                        Instruction prevInst = globalInstructions[instructionIndex - 1];
                        if (prevInst.OpCode.Code == Code.Ldtoken)
                        {
                            IField rvaField = (IField)prevInst.Operand;
                            FieldDef ravFieldDef = rvaField.ResolveFieldDefThrow();
                            if (ravFieldDef.Module != method.Module)
                            {
                                return false;
                            }
                            byte[] data = ravFieldDef.InitialValue;
                            if (data != null && data.Length > 0 && _dataObfuscatorPolicy.NeedObfuscateArray(method, currentInLoop, data))
                            {
                                // don't need cache for byte array obfuscation
                                needCache = false;
                                _dataObfuscator.ObfuscateBytes(method, needCache, ravFieldDef, data, outputInstructions);
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
