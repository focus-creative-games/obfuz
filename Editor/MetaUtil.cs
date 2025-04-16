using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public static class MetaUtil
    {
        public static string GetModuleNameWithoutExt(string moduleName)
        {
            return Path.GetFileNameWithoutExtension(moduleName);
        }

        public static (string, string) SplitNamespaceAndName(string fullName)
        {
            int index = fullName.LastIndexOf('/');
            if (index == -1)
            {
                int index2 = fullName.IndexOf('.');
                return index2 >= 0 ? (fullName.Substring(0, index2), fullName.Substring(index2 + 1)) : ("", fullName);
            }
            return ("", fullName.Substring(index + 1));
        }


        public static TypeDef GetBaseTypeDef(TypeDef type)
        {
            ITypeDefOrRef baseType = type.BaseType;
            if (baseType == null)
            {
                return null;
            }
            TypeDef baseTypeDef = baseType.ResolveTypeDef();
            if (baseTypeDef != null)
            {
                return baseTypeDef;
            }
            if (baseType is TypeSpec baseTypeSpec)
            {
                GenericInstSig genericIns = baseTypeSpec.TypeSig.ToGenericInstSig();
                return genericIns.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
            }
            else
            {
                throw new Exception($"GetBaseTypeDef: {type} fail");
            }
        }

        public static TypeDef GetTypeDefOrGenericTypeBaseThrowException(ITypeDefOrRef type)
        {
            if (type.IsTypeDef)
            {
                return (TypeDef)type;
            }
            if (type.IsTypeRef)
            {
                return type.ResolveTypeDefThrow();
            }
            if (type.IsTypeSpec)
            {
                GenericInstSig gis = type.TryGetGenericInstSig();
                return gis.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
            }
            throw new NotSupportedException($"{type}");
        }

        public static TypeDef GetTypeDefOrGenericTypeBaseOrNull(ITypeDefOrRef type)
        {
            if (type.IsTypeDef)
            {
                return (TypeDef)type;
            }
            if (type.IsTypeRef)
            {
                return type.ResolveTypeDefThrow();
            }
            if (type.IsTypeSpec)
            {
                GenericInstSig gis = type.TryGetGenericInstSig();
                return gis.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
            }
            return null;
        }

        public static TypeDef GetMemberRefTypeDefParentOrNull(IMemberRefParent parent)
        {
            if (parent is TypeDef typeDef)
            {
                return typeDef;
            }
            if (parent is TypeRef typeRef)
            {
                return typeRef.ResolveTypeDefThrow();
            }
            if (parent is TypeSpec typeSpec)
            {
                GenericInstSig genericIns = typeSpec.TypeSig.ToGenericInstSig();
                return genericIns.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
            }
            return null;
        }

        public static bool IsInheritFromUnityObject(TypeDef typeDef)
        {
            TypeDef cur = typeDef;
            while (true)
            {
                cur = GetBaseTypeDef(cur);
                if (cur == null)
                {
                    return false;
                }
                if (cur.Name == "Object" && cur.Namespace == "UnityEngine" && cur.Module.Name == "UnityEngine.CoreModule.dll")
                {
                    return true;
                }
            }
        }



        public static bool IsScriptOrSerializableType(TypeDef type)
        {
            if (type.ContainsGenericParameter)
            {
                return false;
            }
            if (type.IsSerializable)
            {
                return true;
            }

            for (TypeDef parentType = GetBaseTypeDef(type); parentType != null; parentType = GetBaseTypeDef(parentType))
            {
                if ((parentType.Name == "MonoBehaviour" || parentType.Name == "ScriptableObject")
                    && parentType.Namespace == "UnityEngine"
                    && parentType.Module.Assembly.Name == "UnityEngine.CoreModule")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSerializableTypeSig(TypeSig typeSig)
        {
            typeSig = typeSig.RemovePinnedAndModifiers();
            switch (typeSig.ElementType)
            {
                case ElementType.Boolean:
                case ElementType.Char:
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                case ElementType.String:
                    return true;
                case ElementType.Class:
                    return IsScriptOrSerializableType(typeSig.ToTypeDefOrRef().ResolveTypeDefThrow());
                case ElementType.ValueType:
                {
                    TypeDef typeDef = typeSig.ToTypeDefOrRef().ResolveTypeDefThrow();
                    if (typeDef.IsEnum)
                    {
                        return true;
                    }
                    return typeDef.IsSerializable;
                }
                case ElementType.GenericInst:
                {
                    GenericInstSig genericIns = typeSig.ToGenericInstSig();
                    TypeDef typeDef = genericIns.GenericType.ToTypeDefOrRef().ResolveTypeDefThrow();
                    return typeDef.FullName == "System.Collections.Generic.List`1" && IsSerializableTypeSig(genericIns.GenericArguments[0]);
                }
                case ElementType.SZArray:
                {
                    return IsSerializableTypeSig(typeSig.RemovePinnedAndModifiers().Next);
                }
                default:
                return false;
            }
        }

        public static bool IsSerializableField(FieldDef field)
        {
            if (field.IsStatic)
            {
                return false;
            }
            var fieldSig = field.FieldSig.Type;
            if (field.IsPublic)
            {
                return IsSerializableTypeSig(fieldSig);
            }
            if (field.CustomAttributes.Any(c => c.TypeFullName == "UnityEngine.SerializeField"))
            {
                //UnityEngine.Debug.Assert(IsSerializableTypeSig(fieldSig));
                return true;
            }
            return false;
        }

        public static bool MayRenameCustomDataType(ElementType type)
        {
            return type == ElementType.Class || type == ElementType.ValueType || type == ElementType.Object || type == ElementType.SZArray;
        }

        public static TypeSig RetargetTypeRefInTypeSig(TypeSig type)
        {
            TypeSig next = type.Next;
            TypeSig newNext = next != null ? RetargetTypeRefInTypeSig(next) : null;
            if (type.IsModifier || type.IsPinned)
            {
                if (next == newNext)
                {
                    return type;
                }
                if (type is CModReqdSig cmrs)
                {
                    return new CModReqdSig(cmrs.Modifier, newNext);
                }
                if (type is CModOptSig cmos)
                {
                    return new CModOptSig(cmos.Modifier, newNext);
                }
                if (type is PinnedSig ps)
                {
                    return new PinnedSig(newNext);
                }
                throw new System.NotSupportedException(type.ToString());
            }
            switch (type.ElementType)
            {
                case ElementType.Ptr:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new PtrSig(newNext);
                }
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    var vts = type as ClassOrValueTypeSig;
                    TypeDef typeDef = vts.TypeDefOrRef.ResolveTypeDefThrow();
                    if (typeDef == vts.TypeDefOrRef)
                    {
                        return type;
                    }
                    return type.IsClassSig ? new ClassSig(typeDef) : new ValueTypeSig(typeDef);
                }
                case ElementType.Array:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new ArraySig(newNext);
                }
                case ElementType.SZArray:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new SZArraySig(newNext);
                }
                case ElementType.GenericInst:
                {
                    var gis = type as GenericInstSig;
                    ClassOrValueTypeSig genericType = gis.GenericType;
                    ClassOrValueTypeSig newGenericType = (ClassOrValueTypeSig)RetargetTypeRefInTypeSig(genericType);
                    bool anyChange = genericType != newGenericType;
                    var genericArgs = new List<TypeSig>();
                    foreach (var arg in gis.GenericArguments)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != genericType;
                        genericArgs.Add(newArg);
                    }
                    if (!anyChange)
                    {
                        return type;
                    }
                    return new GenericInstSig(newGenericType, genericArgs);
                }
                case ElementType.FnPtr:
                {
                    var fp = type as FnPtrSig;
                    MethodSig methodSig = fp.MethodSig;
                    TypeSig newReturnType = RetargetTypeRefInTypeSig(methodSig.RetType);
                    bool anyChange = newReturnType != methodSig.RetType;
                    var newArgs = new List<TypeSig>();
                    foreach (TypeSig arg in methodSig.Params)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != newReturnType;
                    }
                    if (!anyChange)
                    {
                        return type;
                    }
                    var newParamsAfterSentinel = new List<TypeSig>();
                    foreach (TypeSig arg in methodSig.ParamsAfterSentinel)
                    {
                        TypeSig newArg = RetargetTypeRefInTypeSig(arg);
                        anyChange |= newArg != arg;
                        newParamsAfterSentinel.Add(newArg);
                    }

                    var newMethodSig = new MethodSig(methodSig.CallingConvention, methodSig.GenParamCount, newReturnType, newArgs, newParamsAfterSentinel);
                    return new FnPtrSig(newMethodSig);
                }
                case ElementType.ByRef:
                {
                    if (next == newNext)
                    {
                        return type;
                    }
                    return new ByRefSig(newNext);
                }
                default:
                {
                    return type;
                }
            }
        }


        public static object RetargetTypeRefInTypeSigOfValue(object oldValue)
        {
            if (oldValue == null)
            {
                return null;
            }
            string typeName = oldValue.GetType().FullName;
            if (oldValue.GetType().IsPrimitive)
            {
                return oldValue;
            }
            if (oldValue is string || oldValue is UTF8String)
            {
                return oldValue;
            }
            if (oldValue is TypeSig typeSig)
            {
                return RetargetTypeRefInTypeSig(typeSig);
            }
            if (oldValue is CAArgument caValue)
            {
                TypeSig newType = RetargetTypeRefInTypeSig(caValue.Type);
                object newValue = RetargetTypeRefInTypeSigOfValue(caValue.Value);
                if (newType != caValue.Type || newValue != caValue.Value)
                {
                    return new CAArgument(newType, newValue);
                }
                return oldValue;
            }
            if (oldValue is List<CAArgument> oldArr)
            {
                bool anyChange = false;
                var newArr = new List<CAArgument>();
                foreach (CAArgument oldArg in oldArr)
                {
                    if (TryRetargetTypeRefInArgument(oldArg, out var newArg))
                    {
                        anyChange = true;
                        newArr.Add(newArg);
                    }
                    else
                    {
                        newArr.Add(oldArg);
                    }
                }
                return anyChange ? newArr : oldArr;
            }
            throw new NotSupportedException($"type:{oldValue.GetType()} value:{oldValue}");
        }



        public static bool TryRetargetTypeRefInArgument(CAArgument oldArg, out CAArgument newArg)
        {
            TypeSig newType = RetargetTypeRefInTypeSig(oldArg.Type);
            object newValue = RetargetTypeRefInTypeSigOfValue(oldArg.Value);
            if (newType != oldArg.Type || oldArg.Value != newValue)
            {
                newArg = new CAArgument(newType, newValue);
                return true;
            }
            newArg = default;
            return false;
        }

        public static bool TryRetargetTypeRefInNamedArgument(CANamedArgument arg)
        {
            bool anyChange = false;
            TypeSig newType = RetargetTypeRefInTypeSig(arg.Type);
            if (newType != arg.Type)
            {
                anyChange = true;
                arg.Type = newType;
            }
            if (TryRetargetTypeRefInArgument(arg.Argument, out var newArg))
            {
                arg.Argument = newArg;
                anyChange = true;
            }
            return anyChange;
        }
    }
}
