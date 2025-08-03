using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    class ObfusMethodContext
    {
        public MethodDef method;
        public LocalVariableAllocator localVariableAllocator;
        public IRandom localRandom;
        public EncryptionScopeInfo encryptionScope;
    }

    public class CallObfusPass : ObfuscationMethodPassBase
    {
        public static CallObfuscationSettingsFacade CurrentSettings { get; private set; }

        private readonly CallObfuscationSettingsFacade _settings;
        private readonly SpecialWhiteListMethodCalculator _specialWhiteListMethodCache;

        private IObfuscator _dynamicProxyObfuscator;
        private IObfuscationPolicy _dynamicProxyPolicy;

        public override ObfuscationPassType Type => ObfuscationPassType.CallObfus;

        public CallObfusPass(CallObfuscationSettingsFacade settings)
        {
            _settings = settings;
            CurrentSettings = settings;

            _specialWhiteListMethodCache = new SpecialWhiteListMethodCalculator(settings.obfuscateCallToMethodInMscorlib);
        }

        public override void Stop()
        {
            _dynamicProxyObfuscator.Done();
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _dynamicProxyObfuscator = CreateObfuscator(ctx, _settings.proxyMode);
            _dynamicProxyPolicy = new ConfigurableObfuscationPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        private IObfuscator CreateObfuscator(ObfuscationPassContext ctx, ProxyMode mode)
        {
            switch (mode)
            {
                case ProxyMode.Dispatch:
                return new DispatchProxyObfuscator(ctx.moduleEntityManager);
                case ProxyMode.Delegate:
                return new DelegateProxyObfuscator(ctx.moduleEntityManager);
                default:
                throw new System.NotSupportedException($"Unsupported proxy mode: {mode}");
            }
        }

        protected override void ObfuscateData(MethodDef method)
        {
            BasicBlockCollection bbc = new BasicBlockCollection(method, false);

            IList<Instruction> instructions = method.Body.Instructions;

            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            var encryptionScope = ctx.moduleEntityManager.EncryptionScopeProvider.GetScope(method.Module);
            var localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
            var omc = new ObfusMethodContext
            {
                method = method,
                localVariableAllocator = new LocalVariableAllocator(method),
                localRandom = localRandom,
                encryptionScope = encryptionScope,
            };
            Instruction lastInst = null;
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                BasicBlock block = bbc.GetBasicBlockByInstruction(inst);
                outputInstructions.Clear();
                if (TryObfuscateInstruction(method, lastInst, inst, outputInstructions, omc))
                {
                    // current instruction may be the target of control flow instruction, so we can't remove it directly.
                    // we replace it with nop now, then remove it in CleanUpInstructionPass
                    inst.OpCode = outputInstructions[0].OpCode;
                    inst.Operand = outputInstructions[0].Operand;
                    totalFinalInstructions.Add(inst);
                    for (int k = 1; k < outputInstructions.Count; k++)
                    {
                        totalFinalInstructions.Add(outputInstructions[k]);
                    }
                }
                else
                {
                    totalFinalInstructions.Add(inst);
                }
                lastInst = inst;
            }

            instructions.Clear();
            foreach (var obInst in totalFinalInstructions)
            {
                instructions.Add(obInst);
            }
        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedObfuscateCallInMethod(method);
        }

        private bool TryObfuscateInstruction(MethodDef callerMethod, Instruction lastInst, Instruction inst, List<Instruction> outputInstructions, ObfusMethodContext ctx)
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
                    if (lastInst != null && lastInst.OpCode.Code == Code.Constrained)
                    {
                        return false;
                    }
                    callVir = true;
                    break;
                }
                default: return false;
            }


            if (_specialWhiteListMethodCache.IsInWhiteList(calledMethod))
            {
                return false;
            }


            if (!_dynamicProxyPolicy.NeedObfuscateCalledMethod(callerMethod, calledMethod, callVir))
            {
                return false;
            }

            return _dynamicProxyObfuscator.Obfuscate(callerMethod, calledMethod, callVir, outputInstructions);
        }
    }
}
