using dnlib.DotNet;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.CallObfus
{
    class SpecialWhiteListMethodCalculator
    {
        private readonly RuntimeType _targetRuntime;
        private readonly bool _obfuscateCallToMethodInMscorlib;
        private readonly CachedDictionary<IMethod, bool> _specialWhiteListMethodCache;

        public SpecialWhiteListMethodCalculator(RuntimeType targetRuntime, bool obfuscateCallToMethodInMscorlib)
        {
            _targetRuntime = targetRuntime;
            _obfuscateCallToMethodInMscorlib = obfuscateCallToMethodInMscorlib;
            _specialWhiteListMethodCache = new CachedDictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes, this.ComputeIsInWhiteList);
        }

        public bool IsInWhiteList(IMethod calledMethod)
        {
            return _specialWhiteListMethodCache.GetValue(calledMethod);
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
            if (_targetRuntime == RuntimeType.Mono)
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

            if (!_obfuscateCallToMethodInMscorlib && typeDef.Module.IsCoreLibraryModule == true)
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
    }
}
