using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Virtualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.DynamicProxy
{
    public class DynamicProxyPass : MethodBodyObfuscationPassBase
    {
        private readonly IDynamicProxyPolicy _dynamicProxyPolicy;
        private readonly IDynamicProxyObfuscator _dynamicProxyObfuscator;

        public DynamicProxyPass()
        {
            _dynamicProxyPolicy = new ConfigDynamicProxyPolicy();
            _dynamicProxyObfuscator = new DefaultDynamicProxyObfuscator();
        }

        public override void Stop(ObfuscatorContext ctx)
        {

        }

        public override void Start(ObfuscatorContext ctx)
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedDynamicProxyCallInMethod(method);
        }

        protected override bool TryObfuscateInstruction(MethodDef method, Instruction inst, IList<Instruction> instructions, int instructionIndex, List<Instruction> obfuscatedInstructions)
        {
            switch (inst.OpCode.Code)
            {
                case Code.Call:
                {
                    IMethod calledMethod = (IMethod)inst.Operand;
                    if (!_dynamicProxyPolicy.NeedDynamicProxyCalledMethod(calledMethod, false))
                    {
                        return false;
                    }
                    _dynamicProxyObfuscator.Obfuscate(method, calledMethod, false, obfuscatedInstructions);
                    return true;
                }
                case Code.Callvirt:
                {
                    if (instructionIndex > 0 && instructions[instructionIndex - 1].OpCode.Code == Code.Constrained)
                    {
                        return false;
                    }
                    IMethod calledMethod = (IMethod)inst.Operand;
                    if (!_dynamicProxyPolicy.NeedDynamicProxyCalledMethod(calledMethod, true))
                    {
                        return false;
                    }
                    _dynamicProxyObfuscator.Obfuscate(method, calledMethod, true, obfuscatedInstructions);
                    return true;
                }
                default: return false;
            }
        }
    }
}
