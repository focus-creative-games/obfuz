// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

﻿using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    // Computes the most specific common supertype of two evaluation-stack type signatures. The result
    // is used as the type of control-flow merge-point locals, so it must always be a supertype of both
    // inputs. Returning a type that is not assignable from one side (or an open generic type) causes
    // il2cpp to emit incompatible C++ pointer assignments, which fail to compile.
    class TypeReducer
    {
        private readonly ModuleDef _module;
        private readonly ICorLibTypes _corLibTypes;

        public TypeReducer(ModuleDef module)
        {
            _module = module;
            _corLibTypes = module.CorLibTypes;
        }

        public TypeSig ReduceBaseType(TypeSig type1, TypeSig type2)
        {
            type1 = type1.RemovePinnedAndModifiers();
            type2 = type2.RemovePinnedAndModifiers();
            if (TypeEqualityComparer.Instance.Equals(type1, type2))
            {
                return type1;
            }

            if (IsReferenceTypeSig(type1) && IsReferenceTypeSig(type2))
            {
                return ReduceReferenceBaseType(type1, type2);
            }
            return ReduceNonReferenceBaseType(type1, type2);
        }

        // Reduce two reference types (class/interface/array/string/object/generic-instance) to the
        // most specific common supertype that both values can be stored into. The result becomes the
        // type of merge-point locals, so it MUST be a supertype of both inputs. Returning the more
        // derived type (or an unrelated sibling) once made il2cpp assign incompatible C++ pointer
        // types to the same variable, producing a compile error.
        private TypeSig ReduceReferenceBaseType(TypeSig type1, TypeSig type2)
        {
            // object is the top of the reference type hierarchy.
            if (type1.ElementType == ElementType.Object)
            {
                return type1;
            }
            if (type2.ElementType == ElementType.Object)
            {
                return type2;
            }

            // string is sealed, so two distinct reference types involving it only share object.
            if (type1.ElementType == ElementType.String || type2.ElementType == ElementType.String)
            {
                return _corLibTypes.Object;
            }

            bool isArray1 = IsArrayTypeSig(type1);
            bool isArray2 = IsArrayTypeSig(type2);
            if (isArray1 && isArray2)
            {
                return ReduceArrayBaseType(type1, type2);
            }
            if (isArray1)
            {
                return ReduceArrayWithType(type1, type2);
            }
            if (isArray2)
            {
                return ReduceArrayWithType(type2, type1);
            }

            // One direction may cover the vast majority of cases, including covariance: e.g.
            // Unit -> Entity, List<Unit> -> IEnumerable<Entity>.
            if (IsAssignableTo(type1, type2))
            {
                return type2;
            }
            if (IsAssignableTo(type2, type1))
            {
                return type1;
            }

            TypeDef def1 = GetTypeDefOrNull(type1);
            TypeDef def2 = GetTypeDefOrNull(type2);
            TypeDef common = FindCommonBaseTypeDef(def1, def2);
            if (common == null || common.HasGenericParameters)
            {
                return _corLibTypes.Object;
            }
            return TypeDefToTypeSig(common);
        }

        private TypeSig ReduceArrayBaseType(TypeSig type1, TypeSig type2)
        {
            bool isVector1 = type1.ElementType == ElementType.SZArray;
            bool isVector2 = type2.ElementType == ElementType.SZArray;

            if (isVector1 && isVector2)
            {
                TypeSig elementType = ReduceArrayElementType(type1.Next, type2.Next);
                return elementType != null ? new SZArraySig(elementType) : _corLibTypes.Object;
            }

            if (type1.ElementType == ElementType.Array && type2.ElementType == ElementType.Array)
            {
                ArraySig array1 = (ArraySig)type1;
                ArraySig array2 = (ArraySig)type2;
                if (array1.Rank != array2.Rank)
                {
                    return SystemArrayTypeSig();
                }
                TypeSig elementType = ReduceArrayElementType(array1.Next, array2.Next);
                return elementType != null ? new ArraySig(elementType, array1.Rank) : _corLibTypes.Object;
            }

            // A vector and a multi-dimensional array only share System.Array.
            return SystemArrayTypeSig();
        }

        private TypeSig ReduceArrayWithType(TypeSig arrayType, TypeSig otherType)
        {
            // Arrays implement System.Array and a set of collection interfaces; if the other type is
            // one of them, the array is already assignable and that interface is the common base.
            if (IsAssignableTo(arrayType, otherType))
            {
                return otherType;
            }

            TypeDef common = FindCommonBaseTypeDef(GetTypeDefOrNull(arrayType), GetTypeDefOrNull(otherType));
            if (common == null || common.HasGenericParameters)
            {
                return _corLibTypes.Object;
            }
            return TypeDefToTypeSig(common);
        }

        private TypeSig ReduceNonReferenceBaseType(TypeSig type1, TypeSig type2)
        {
            ElementType e1 = type1.ElementType;
            ElementType e2 = type2.ElementType;

            if (IsNativeIntOrPointer(e1) && IsNativeIntOrPointer(e2))
            {
                return ReduceNativeIntBaseType(type1, type2);
            }

            if (IsNumeric(e1) && IsNumeric(e2))
            {
                return ReduceNumericBaseType(e1, e2);
            }

            // Two unrelated value types share no common local type (they cannot be merged in valid
            // IL either). Preserve the original type rather than crashing on pre-existing bad input;
            // this branch is unreachable for verifiable methods.
            UnityEngine.Debug.LogError($"reduce base type of {type1} and {type2} is unsupported");
            return type1;
        }

        private TypeSig ReduceNativeIntBaseType(TypeSig type1, TypeSig type2)
        {
            ElementType e1 = type1.ElementType;
            ElementType e2 = type2.ElementType;
            if (e1 == ElementType.ByRef && e2 == ElementType.ByRef)
            {
                return new ByRefSig(ReduceBaseType(type1.Next, type2.Next));
            }
            if (e1 == ElementType.Ptr && e2 == ElementType.Ptr)
            {
                return new PtrSig(ReduceBaseType(type1.Next, type2.Next));
            }
            // FnPtr / native int / mixed pointer-vs-native-int all collapse to a native-sized int.
            return _corLibTypes.IntPtr;
        }

        private TypeSig ReduceNumericBaseType(ElementType e1, ElementType e2)
        {
            // Normalize so that, when one side is a small integer and the other is not, the small
            // integer is e1. This keeps the widening rules below symmetric without order coupling.
            if (IsSmallInt(e2) && !IsSmallInt(e1))
            {
                (e1, e2) = (e2, e1);
            }

            if (IsSmallInt(e1))
            {
                if (IsSmallInt(e2))
                {
                    return _corLibTypes.Int32;
                }
                if (e2 == ElementType.I8)
                {
                    return _corLibTypes.Int64;
                }
                if (e2 == ElementType.U8)
                {
                    return _corLibTypes.UInt64;
                }
                if (e2 == ElementType.R4)
                {
                    return _corLibTypes.Single;
                }
                if (e2 == ElementType.R8)
                {
                    return _corLibTypes.Double;
                }
                return _corLibTypes.Object;
            }

            if (e1 == ElementType.I8 || e1 == ElementType.U8)
            {
                if (e2 == ElementType.I8 || e2 == ElementType.U8)
                {
                    return e1 == ElementType.U8 && e2 == ElementType.U8 ? _corLibTypes.UInt64 : _corLibTypes.Int64;
                }
                if (e2 == ElementType.R8)
                {
                    return _corLibTypes.Double;
                }
                return _corLibTypes.Object;
            }

            if (e1 == ElementType.R4)
            {
                if (e2 == ElementType.R8)
                {
                    return _corLibTypes.Double;
                }
                return _corLibTypes.Object;
            }

            if (e1 == ElementType.R8 && e2 == ElementType.R8)
            {
                return _corLibTypes.Double;
            }

            return _corLibTypes.Object;
        }

        private TypeSig ReduceArrayElementType(TypeSig element1, TypeSig element2)
        {
            element1 = element1.RemovePinnedAndModifiers();
            element2 = element2.RemovePinnedAndModifiers();
            if (TypeEqualityComparer.Instance.Equals(element1, element2))
            {
                return element1;
            }
            // Arrays are only covariant over reference element types.
            if (IsReferenceTypeSig(element1) && IsReferenceTypeSig(element2))
            {
                return ReduceBaseType(element1, element2);
            }
            return null;
        }

        private bool IsAssignableTo(TypeSig from, TypeSig to)
        {
            from = from.RemovePinnedAndModifiers();
            to = to.RemovePinnedAndModifiers();

            if (TypeEqualityComparer.Instance.Equals(from, to))
            {
                return true;
            }
            if (!IsReferenceTypeSig(from))
            {
                return false;
            }
            if (to.ElementType == ElementType.Object)
            {
                return true;
            }
            if (from.ElementType == ElementType.Object)
            {
                return false;
            }
            // string is sealed.
            if (from.ElementType == ElementType.String)
            {
                return false;
            }

            if (from.ElementType == ElementType.SZArray && to.ElementType == ElementType.SZArray)
            {
                return IsReferenceTypeSig(from.Next) && IsAssignableTo(from.Next, to.Next);
            }

            if (IsArrayTypeSig(from) && IsArrayAssignableToType(from, to))
            {
                return true;
            }

            if (from.ElementType == ElementType.GenericInst)
            {
                return IsGenericInstAssignableTo((GenericInstSig)from, to);
            }

            if (to.ElementType == ElementType.GenericInst)
            {
                return false;
            }

            return IsTypeDefInClosure(GetTypeDefOrNull(from), GetTypeDefOrNull(to));
        }

        private bool IsArrayAssignableToType(TypeSig fromArray, TypeSig to)
        {
            bool isVector = fromArray.ElementType == ElementType.SZArray;

            TypeDef toDef = GetTypeDefOrNull(to);
            if (toDef != null)
            {
                string ns = toDef.Namespace;
                string name = toDef.Name;
                if (ns == "System")
                {
                    if (name == "Array" || name == "ICloneable")
                    {
                        return true;
                    }
                }
                else if (ns == "System.Collections")
                {
                    if (name == "IList" || name == "ICollection" || name == "IEnumerable"
                        || name == "IStructuralComparable" || name == "IStructuralEquatable")
                    {
                        return true;
                    }
                }
            }

            if (isVector && to.ElementType == ElementType.GenericInst)
            {
                GenericInstSig gis = (GenericInstSig)to;
                TypeDef genericDef = gis.GenericType?.TypeDefOrRef?.ResolveTypeDef();
                if (genericDef != null && genericDef.Namespace == "System.Collections.Generic")
                {
                    string name = genericDef.Name;
                    if (name == "IList`1" || name == "ICollection`1" || name == "IEnumerable`1"
                        || name == "IReadOnlyList`1" || name == "IReadOnlyCollection`1")
                    {
                        return gis.GenericArguments.Count == 1
                            && TypeEqualityComparer.Instance.Equals(fromArray.Next, gis.GenericArguments[0]);
                    }
                }
            }
            return false;
        }

        private bool IsGenericInstAssignableTo(GenericInstSig from, TypeSig to)
        {
            TypeDef fromDef = from.GenericType?.TypeDefOrRef?.ResolveTypeDef();
            if (fromDef == null)
            {
                return false;
            }

            if (to.ElementType == ElementType.GenericInst)
            {
                GenericInstSig toGis = (GenericInstSig)to;
                TypeDef toDef = toGis.GenericType?.TypeDefOrRef?.ResolveTypeDef();
                if (toDef != null && TypeDefEquals(fromDef, toDef))
                {
                    return GenericArgumentsAssignable(from.GenericArguments, toGis.GenericArguments, fromDef);
                }
                return IsGenericInstAssignableToInflated(from, to);
            }

            return IsTypeDefInClosure(fromDef, GetTypeDefOrNull(to));
        }

        private bool IsGenericInstAssignableToInflated(GenericInstSig from, TypeSig to)
        {
            TypeDef fromDef = from.GenericType?.TypeDefOrRef?.ResolveTypeDef();
            if (fromDef == null)
            {
                return false;
            }

            GenericArgumentContext gac = BuildGenericArgumentContext(fromDef, from.GenericArguments);

            if (fromDef.BaseType != null)
            {
                TypeSig inflatedBase = InflateSigSafe(gac, fromDef.BaseType.ToTypeSig());
                if (IsAssignableTo(inflatedBase, to))
                {
                    return true;
                }
            }
            foreach (InterfaceImpl ifaceImpl in fromDef.Interfaces)
            {
                TypeSig inflatedInterface = InflateSigSafe(gac, ifaceImpl.Interface.ToTypeSig());
                if (IsAssignableTo(inflatedInterface, to))
                {
                    return true;
                }
            }
            return false;
        }

        private bool GenericArgumentsAssignable(IList<TypeSig> fromArgs, IList<TypeSig> toArgs, TypeDef genericTypeDef)
        {
            if (fromArgs.Count != toArgs.Count)
            {
                return false;
            }
            for (int i = 0; i < fromArgs.Count; i++)
            {
                GenericParam gp = genericTypeDef.GenericParameters[i];
                switch (gp.Variance)
                {
                case GenericParamAttributes.Covariant:
                    if (!IsAssignableTo(fromArgs[i], toArgs[i]))
                    {
                        return false;
                    }
                    break;
                case GenericParamAttributes.Contravariant:
                    if (!IsAssignableTo(toArgs[i], fromArgs[i]))
                    {
                        return false;
                    }
                    break;
                default:
                    if (!TypeEqualityComparer.Instance.Equals(fromArgs[i], toArgs[i]))
                    {
                        return false;
                    }
                    break;
                }
            }
            return true;
        }

        private GenericArgumentContext BuildGenericArgumentContext(TypeDef genericTypeDef, IList<TypeSig> arguments)
        {
            var typeArgs = new List<TypeSig>();
            foreach (GenericParam gp in genericTypeDef.GenericParameters)
            {
                int number = gp.Number;
                typeArgs.Add(number < arguments.Count ? arguments[number] : null);
            }
            return new GenericArgumentContext(typeArgs, null);
        }

        private static TypeSig InflateSigSafe(GenericArgumentContext ctx, TypeSig sig)
        {
            try
            {
                return ctx.Resolve(sig);
            }
            catch
            {
                return sig;
            }
        }

        private bool IsTypeDefInClosure(TypeDef fromDef, TypeDef toDef)
        {
            if (fromDef == null || toDef == null)
            {
                return false;
            }
            var closure = new List<TypeDef>();
            CollectBaseTypeAndInterfaces(fromDef, closure);
            return ContainsTypeDef(closure, toDef);
        }

        private TypeDef FindCommonBaseTypeDef(TypeDef a, TypeDef b)
        {
            if (a == null || b == null)
            {
                return null;
            }
            if (TypeDefEquals(a, b))
            {
                return a;
            }

            var ancestorsOfA = new List<TypeDef>();
            CollectBaseTypeAndInterfaces(a, ancestorsOfA);

            var visited = new List<TypeDef>();
            var queue = new Queue<TypeDef>();
            queue.Enqueue(b);
            visited.Add(b);
            while (queue.Count > 0)
            {
                TypeDef cur = queue.Dequeue();
                if (ContainsTypeDef(ancestorsOfA, cur))
                {
                    return cur;
                }
                TypeDef baseType = GetBaseTypeDefSafe(cur);
                if (baseType != null && !ContainsTypeDef(visited, baseType))
                {
                    visited.Add(baseType);
                    queue.Enqueue(baseType);
                }
                foreach (InterfaceImpl ifaceImpl in cur.Interfaces)
                {
                    TypeDef iface = ifaceImpl.Interface?.ResolveTypeDef();
                    if (iface != null && !ContainsTypeDef(visited, iface))
                    {
                        visited.Add(iface);
                        queue.Enqueue(iface);
                    }
                }
            }
            return null;
        }

        private void CollectBaseTypeAndInterfaces(TypeDef type, List<TypeDef> result)
        {
            if (type == null || ContainsTypeDef(result, type))
            {
                return;
            }
            result.Add(type);
            CollectBaseTypeAndInterfaces(GetBaseTypeDefSafe(type), result);
            foreach (InterfaceImpl ifaceImpl in type.Interfaces)
            {
                CollectBaseTypeAndInterfaces(ifaceImpl.Interface?.ResolveTypeDef(), result);
            }
        }

        private TypeDef GetBaseTypeDefSafe(TypeDef type)
        {
            if (type == null || type.BaseType == null)
            {
                return null;
            }
            try
            {
                return MetaUtil.GetTypeDefOrGenericTypeBaseOrNull(type.BaseType);
            }
            catch
            {
                return null;
            }
        }

        private TypeDef GetTypeDefOrNull(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
            case ElementType.Class:
            case ElementType.ValueType:
                return type.ToTypeDefOrRef()?.ResolveTypeDef();
            case ElementType.GenericInst:
                return ((GenericInstSig)type).GenericType?.TypeDefOrRef?.ResolveTypeDef();
            case ElementType.SZArray:
            case ElementType.Array:
                return SystemArrayTypeDef();
            default:
                return null;
            }
        }

        private TypeSig TypeDefToTypeSig(TypeDef typeDef)
        {
            if (typeDef == null)
            {
                return _corLibTypes.Object;
            }
            if (typeDef.HasGenericParameters)
            {
                return _corLibTypes.Object;
            }
            if (typeDef.Namespace == "System" && typeDef.Name == "Object" && typeDef.DefinitionAssembly?.IsCorLib() == true)
            {
                return _corLibTypes.Object;
            }
            return _module.Import(typeDef).ToTypeSig();
        }

        private TypeDef SystemArrayTypeDef()
        {
            return SystemArrayTypeSig().ToTypeDefOrRef()?.ResolveTypeDef();
        }

        private TypeSig SystemArrayTypeSig()
        {
            return _module.ImportAsTypeSig(typeof(Array));
        }

        private bool IsReferenceTypeSig(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
            case ElementType.String:
            case ElementType.Class:
            case ElementType.Object:
            case ElementType.SZArray:
            case ElementType.Array:
                return true;
            case ElementType.GenericInst:
                return !MetaUtil.IsValueType(type);
            default:
                return false;
            }
        }

        private static bool IsArrayTypeSig(TypeSig type)
        {
            return type.ElementType == ElementType.SZArray || type.ElementType == ElementType.Array;
        }

        private static bool IsNativeIntOrPointer(ElementType type)
        {
            return type == ElementType.I || type == ElementType.U || type == ElementType.Ptr
                || type == ElementType.FnPtr || type == ElementType.ByRef;
        }

        private static bool IsNumeric(ElementType type)
        {
            switch (type)
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
                return true;
            default:
                return false;
            }
        }

        private static bool IsSmallInt(ElementType type)
        {
            switch (type)
            {
            case ElementType.Boolean:
            case ElementType.Char:
            case ElementType.I1:
            case ElementType.U1:
            case ElementType.I2:
            case ElementType.U2:
            case ElementType.I4:
            case ElementType.U4:
                return true;
            default:
                return false;
            }
        }

        private static bool TypeDefEquals(TypeDef a, TypeDef b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }
            return a.FullName == b.FullName && a.DefinitionAssembly?.FullName == b.DefinitionAssembly?.FullName;
        }

        private static bool ContainsTypeDef(List<TypeDef> list, TypeDef type)
        {
            if (type == null)
            {
                return false;
            }
            foreach (TypeDef item in list)
            {
                if (TypeDefEquals(item, type))
                {
                    return true;
                }
            }
            return false;
        }
    }
}