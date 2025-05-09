using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using Obfuz.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Obfuz.Settings;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class CallObfusPass : InstructionObfuscationPassBase
    {
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly ICallObfusPolicy _dynamicProxyPolicy;
        private readonly ICallObfuscator _dynamicProxyObfuscator;

        public CallObfusPass(CallObfusSettings settings)
        {
            _random = new RandomWithKey(new byte[] { 0x1, 0x2, 0x3, 0x4 }, 0x5);
            _encryptor = new DefaultEncryptor(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D });
            _dynamicProxyPolicy = new ConfigurableCallObfusPolicy();
            _dynamicProxyObfuscator = new DefaultProxyCallObfuscator(_random, _encryptor);
        }

        public override void Stop(ObfuscationPassContext ctx)
        {
            _dynamicProxyObfuscator.Done();
        }

        public override void Start(ObfuscationPassContext ctx)
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedDynamicProxyCallInMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef method, Instruction inst, IList<Instruction> instructions, int instructionIndex,
            List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
        {
            IMethod calledMethod = inst.Operand as IMethod;
            if (calledMethod == null || !calledMethod.IsMethod)
            {
                return false;
            }
            if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
            {
                return false;
            }

            switch (inst.OpCode.Code)
            {
                case Code.Call:
                {
                    if (!_dynamicProxyPolicy.NeedDynamicProxyCalledMethod(calledMethod, false))
                    {
                        return false;
                    }
                    _dynamicProxyObfuscator.Obfuscate(method, calledMethod, false, outputInstructions);
                    return true;
                }
                case Code.Callvirt:
                {
                    if (instructionIndex > 0 && instructions[instructionIndex - 1].OpCode.Code == Code.Constrained)
                    {
                        return false;
                    }
                    if (!_dynamicProxyPolicy.NeedDynamicProxyCalledMethod(calledMethod, true))
                    {
                        return false;
                    }
                    _dynamicProxyObfuscator.Obfuscate(method, calledMethod, true, outputInstructions);
                    return true;
                }
                default: return false;
            }
        }
    }
}
