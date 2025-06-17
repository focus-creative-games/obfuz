using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.ObfusPasses.ExprObfus.Obfuscators;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obfuz.ObfusPasses.ExprObfus
{
    class ExprObfusPass : ObfuscationMethodPassBase
    {
        private readonly ExprObfuscationSettingsFacade _settings;
        private IObfuscationPolicy _obfuscationPolicy;
        private IObfuscator _obfuscator;

        public ExprObfusPass(ExprObfuscationSettingsFacade settings)
        {
            _settings = settings;
        }

        public override ObfuscationPassType Type => ObfuscationPassType.ExprObfus;

        public override void Start()
        {
            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            _obfuscationPolicy = new ConfigurableObfuscationPolicy(
                ctx.coreSettings.assembliesToObfuscate,
                _settings.ruleFiles);
            _obfuscator = CreateObfuscator(ctx.encryptionScopeProvider, ctx.moduleEntityManager, _settings.obfuscationLevel);
        }

        private IObfuscator CreateObfuscator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager, ObfuscationLevel level)
        {
            switch (level)
            {
                case ObfuscationLevel.None: return new NoneObfuscator();
                case ObfuscationLevel.Basic:return new BasicObfuscator(encryptionScopeProvider, moduleEntityManager);
                case ObfuscationLevel.Advanced: return new AdvancedObfuscator(encryptionScopeProvider, moduleEntityManager);
                case ObfuscationLevel.MostAdvanced: return new MostAdvancedObfuscator(encryptionScopeProvider, moduleEntityManager);
                default: throw new System.ArgumentOutOfRangeException(nameof(level), level, "Unknown obfuscation level");
            }
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _settings.obfuscationLevel != ObfuscationLevel.None && _obfuscationPolicy.NeedObfuscate(method);
        }

        protected  bool TryObfuscateInstruction(MethodDef callingMethod, InstructionParameterInfo pi, LocalVariableAllocator localVariableAllocator, IRandom localRandom, Instruction inst, List<Instruction> outputInstructions)
        {
            Debug.Log($"Obfuscating instruction: {inst} in method: {callingMethod.FullName}");
            switch (inst.OpCode.Code)
            {
                case Code.Neg:
                {
                    return localRandom.NextInPercentage(_settings.obfuscationPercentage) && _obfuscator.ObfuscateBasicUnaryOp(callingMethod, inst, pi.op1, pi.retType, localVariableAllocator, outputInstructions);
                }
                case Code.Add:
                case Code.Sub:
                case Code.Mul:
                case Code.Div:
                case Code.Div_Un:
                case Code.Rem:
                case Code.Rem_Un:
                {
                    return localRandom.NextInPercentage(_settings.obfuscationPercentage) && _obfuscator.ObfuscateBasicBinOp(callingMethod, inst, pi.op1, pi.op2, pi.retType, localVariableAllocator, outputInstructions);
                }
                case Code.And:
                case Code.Or:
                case Code.Xor:
                {
                    return localRandom.NextInPercentage(_settings.obfuscationPercentage) && _obfuscator.ObfuscateBinBitwiseOp(callingMethod, inst, pi.op1, pi.op2, pi.retType, localVariableAllocator, outputInstructions);
                }
                case Code.Not:
                {
                    return localRandom.NextInPercentage(_settings.obfuscationPercentage) && _obfuscator.ObfuscateUnaryBitwiseOp(callingMethod, inst, pi.op1, pi.retType, localVariableAllocator, outputInstructions);
                }
                case Code.Shl:
                case Code.Shr:
                case Code.Shr_Un:
                {
                    return localRandom.NextInPercentage(_settings.obfuscationPercentage) && _obfuscator.ObfuscateBitShiftOp(callingMethod, inst, pi.op1, pi.op2, pi.retType, localVariableAllocator, outputInstructions);
                }
            }
            return false;
        }

        protected override void ObfuscateData(MethodDef method)
        {
            Debug.Log($"Obfuscating method: {method.FullName} with ExprObfusPass");
            var calc = new EvalStackCalculator(method);
            var localVarAllocator = new LocalVariableAllocator(method);
            IList<Instruction> instructions = method.Body.Instructions;
            var outputInstructions = new List<Instruction>();
            var totalFinalInstructions = new List<Instruction>();
            var encryptionScope = ObfuscationPassContext.Current.encryptionScopeProvider.GetScope(method.Module);
            var localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
            for (int i = 0; i < instructions.Count; i++)
            {
                Instruction inst = instructions[i];
                bool add = false;
                if (calc.TryGetParameterInfo(inst, out InstructionParameterInfo pi))
                {
                    outputInstructions.Clear();
                    if (TryObfuscateInstruction(method, pi, localVarAllocator, localRandom, inst, outputInstructions))
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
                        add = true;
                    }
                }
                if (!add)
                {
                    totalFinalInstructions.Add(inst);
                }
            }

            instructions.Clear();
            foreach (var obInst in totalFinalInstructions)
            {
                instructions.Add(obInst);
            }
        }
    }
}
