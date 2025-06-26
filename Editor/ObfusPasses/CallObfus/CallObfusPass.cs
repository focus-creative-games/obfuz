using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class CallObfusPass : BasicBlockObfuscationPassBase
    {
        private readonly CallObfuscationSettingsFacade _settings;
        private IObfuscator _dynamicProxyObfuscator;
        private IObfuscationPolicy _dynamicProxyPolicy;
        private readonly CachedDictionary<IMethod, bool> _specialwhiteListMethodCache;

        public override ObfuscationPassType Type => ObfuscationPassType.CallObfus;

        public CallObfusPass(CallObfuscationSettingsFacade settings)
        {
            _settings = settings;
            _specialwhiteListMethodCache = new CachedDictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes, this.ComputeIsInWhiteList);
        }

        public override void Stop()
        {
            _dynamicProxyObfuscator.Done();
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _dynamicProxyObfuscator = new DefaultCallProxyObfuscator(ctx.encryptionScopeProvider, ctx.constFieldAllocator, ctx.moduleEntityManager, _settings);
            _dynamicProxyPolicy = new ConfigurableObfuscationPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _dynamicProxyPolicy.NeedObfuscateCallInMethod(method);
        }


        private static readonly HashSet<string> _specialTypeFullNames = new HashSet<string>
        {
            "System.Enum",
            "System.Delegate",
            "System.MulticastDelegate",
            "Obfuz.EncryptionService`1",
        };

        private static readonly HashSet<string> _specialMethodNames = new HashSet<string>
        {
            "GetEnumerator", // List<T>.Enumerator.GetEnumerator()
            ".ctor", // constructor
        };

        private static readonly HashSet<string> _specialMethodFullNames = new HashSet<string>
        {
            "System.Reflection.MethodBase.GetCurrentMethod",
            "System.Reflection.Assembly.GetCallingAssembly",
            "System.Reflection.Assembly.GetExecutingAssembly",
            "System.Reflection.Assembly.GetEntryAssembly",
        };

        private bool ComputeIsInWhiteList(IMethod calledMethod)
        {
            MethodDef calledMethodDef = calledMethod.ResolveMethodDef();
            // mono has more strict access control, calls non-public method will raise exception.
            if (PlatformUtil.IsMonoBackend())
            {
                if (calledMethodDef != null && (!calledMethodDef.IsPublic || !IsTypeSelfAndParentPublic(calledMethodDef.DeclaringType)))
                {
                    return true;
                }
            }

            ITypeDefOrRef declaringType = calledMethod.DeclaringType;
            TypeSig declaringTypeSig = calledMethod.DeclaringType.ToTypeSig();
            declaringTypeSig = declaringTypeSig.RemovePinnedAndModifiers();
            switch (declaringTypeSig.ElementType)
            {
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    break;
                }
                case ElementType.GenericInst:
                {
                    if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
                    {
                        return true;
                    }
                    break;
                }
                default: return true;
            }

            TypeDef typeDef = declaringType.ResolveTypeDef();

            if (!_settings.obfuscateCallToMethodInMscorlib && typeDef.Module.IsCoreLibraryModule == true)
            {
                return true;
            }

            if (typeDef.IsDelegate || typeDef.IsEnum)
                return true;

            string fullName = typeDef.FullName;
            if (_specialTypeFullNames.Contains(fullName))
            {
                return true;
            }
            //if (fullName.StartsWith("System.Runtime.CompilerServices."))
            //{
            //    return true;
            //}

            string methodName = calledMethod.Name;
            if (_specialMethodNames.Contains(methodName))
            {
                return true;
            }

            string methodFullName = $"{fullName}.{methodName}";
            if (_specialMethodFullNames.Contains(methodFullName))
            {
                return true;
            }
            return false;
        }

        private bool IsTypeSelfAndParentPublic(TypeDef type)
        {
            if (type.DeclaringType != null && !IsTypeSelfAndParentPublic(type.DeclaringType))
            {
                return false;
            }

            return type.IsPublic;
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


            if (_specialwhiteListMethodCache.GetValue(calledMethod))
            {
                return false;
            }


            if (!_dynamicProxyPolicy.NeedObfuscateCalledMethod(callerMethod, calledMethod, callVir, block.inLoop))
            {
                return false;
            }

            ObfuscationCachePolicy cachePolicy = _dynamicProxyPolicy.GetMethodObfuscationCachePolicy(callerMethod);
            bool cachedCallIndex = block.inLoop ? cachePolicy.cacheInLoop : cachePolicy.cacheNotInLoop;
            _dynamicProxyObfuscator.Obfuscate(callerMethod, calledMethod, callVir, cachedCallIndex, outputInstructions);
            return true;
        }
    }
}
