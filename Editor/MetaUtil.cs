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

        public static TypeDef GetTypeDefOrGenericTypeBase(ITypeDefOrRef type)
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
    }
}
