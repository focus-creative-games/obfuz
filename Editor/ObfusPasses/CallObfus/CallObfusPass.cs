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
    public class CallObfusPass : BasicBlockObfuscationPassBase
    {
        private readonly List<string> _configFiles;
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly IObfuscator _dynamicProxyObfuscator;
        private IObfuscationPolicy _dynamicProxyPolicy;

        public CallObfusPass(CallObfusSettings settings)
        {
            _configFiles = settings.configFiles.ToList();
            _random = new RandomWithKey(new byte[] { 0x1, 0x2, 0x3, 0x4 }, 0x5);
            _encryptor = new DefaultEncryptor(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D });
            _dynamicProxyObfuscator = new DefaultCallProxyObfuscator(_random, _encryptor);
        }

        public override void Stop(ObfuscationPassContext ctx)
        {
            _dynamicProxyObfuscator.Done();
        }

        public override void Start(ObfuscationPassContext ctx)
        {
            _dynamicProxyPolicy = new ConfigurableObfuscationPolicy(ctx.toObfuscatedAssemblyNames, _configFiles);
        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedObfuscateCallInMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef callerMethod, Instruction inst, BasicBlock block,
            int instructionIndex, IList<Instruction> globalInstructions, List<Instruction> outputInstructions, List<Instruction> totalFinalInstructions)
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

            bool callVir;
            switch (inst.OpCode.Code)
            {
                case Code.Call:
                {
                    callVir = false;
                    break;
                }
                case Code.Callvirt:
                {
                    if (instructionIndex > 0 && globalInstructions[instructionIndex - 1].OpCode.Code == Code.Constrained)
                    {
                        return false;
                    }
                    callVir = true;
                    break;
                }
                default: return false;
            }

            ObfuscationCachePolicy cachePolicy = _dynamicProxyPolicy.GetMethodObfuscationCachePolicy(callerMethod);
            bool cachedCallIndex = block.inLoop ? cachePolicy.cacheInLoop : cachePolicy.cacheNotInLoop;

            if (!_dynamicProxyPolicy.NeedObfuscateCalledMethod(callerMethod, calledMethod, callVir, cachedCallIndex))
            {
                return false;
            }
            _dynamicProxyObfuscator.Obfuscate(callerMethod, calledMethod, callVir, outputInstructions);
            return true;
        }
    }
}
