using dnlib.DotNet;
using Obfuz.Editor;
using Obfuz.Utils;
using System.Linq;
using UnityEngine;

namespace Obfuz
{
    public class ObfuscationMethodWhitelist
    {
        private readonly ObfuzIgnoreScopeComputeCache _obfuzComputeCache;
        private readonly BurstCompileComputeCache _burstCompileComputeCache;

        public ObfuscationMethodWhitelist(ObfuzIgnoreScopeComputeCache obfuzComputeCache, BurstCompileComputeCache burstCompileComputeCache)
        {
            _obfuzComputeCache = obfuzComputeCache;
            _burstCompileComputeCache = burstCompileComputeCache;
        }

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == ConstValues.ObfuzRuntimeAssemblyName)
            {
                return true;
            }
            //if (MetaUtil.HasObfuzIgnoreScope(module))
            //{
            //    return true;
            //}
            return false;
        }

        private bool DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(MethodDef method)
        {
            CustomAttribute ca = method.CustomAttributes.Find(ConstValues.RuntimeInitializedOnLoadMethodAttributeFullName);
            if (ca != null && ca.ConstructorArguments.Count > 0)
            {
                RuntimeInitializeLoadType loadType = (RuntimeInitializeLoadType)ca.ConstructorArguments[0].Value;
                if (loadType >= RuntimeInitializeLoadType.AfterAssembliesLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsInWhiteList(MethodDef method)
        {
            TypeDef typeDef = method.DeclaringType;
            //if (IsInWhiteList(typeDef))
            //{
            //    return true;
            //}
            if (method.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (_obfuzComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(method, typeDef, ObfuzScope.MethodBody))
            {
                return true;
            }
            CustomAttribute ca = method.CustomAttributes.Find(ConstValues.RuntimeInitializedOnLoadMethodAttributeFullName);
            if (DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(method))
            {
                return true;
            }
            if (MetaUtil.HasBurstCompileAttribute(method) || _burstCompileComputeCache.IsBurstCompileMethodOrReferencedByBurstCompileMethod(method))
            {
                return true;
            }

            // don't obfuscate cctor when it has RuntimeInitializeOnLoadMethodAttribute with load type AfterAssembliesLoaded
            if (method.IsStatic && method.Name == ".cctor" && typeDef.Methods.Any(m => DoesMethodContainsRuntimeInitializeOnLoadMethodAttributeAndLoadTypeGreaterEqualAfterAssembliesLoaded(m)))
            {
                return true;
            }
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            if (type.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (IsInWhiteList(type.Module))
            {
                return true;
            }
            if (MetaUtil.HasBurstCompileAttribute(type))
            {
                return true;
            }
            if (_obfuzComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(type, type.DeclaringType, ObfuzScope.MethodBody))
            {
                return true;
            }
            //if (type.DeclaringType != null && IsInWhiteList(type.DeclaringType))
            //{
            //    return true;
            //}
            if (type.FullName == ConstValues.GeneratedEncryptionVirtualMachineFullName)
            {
                return true;
            }
            return false;
        }
    }
}
