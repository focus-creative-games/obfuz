using dnlib.DotNet;
using Obfuz.Rename;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Obfuz
{

    public class SymbolRename
    {
        private readonly ObfuscatorContext _ctx;

        private readonly IRenamePolicy _renamePolicy;
        private readonly INameMaker _nameMaker;
        private readonly Dictionary<ModuleDef, List<CustomAttributeInfo>> _customAttributeArgumentsWithTypeByMods = new Dictionary<ModuleDef, List<CustomAttributeInfo>>();


        class CustomAttributeInfo
        {
            public CustomAttributeCollection customAttributes;
            public int index;
            public List<CAArgument> arguments;
            public List<CANamedArgument> namedArguments;
        }
        public SymbolRename(ObfuscatorContext ctx)
        {
            _ctx = ctx;
            _renamePolicy = ctx.renamePolicy;
            _nameMaker = ctx.nameMaker;
            BuildCustomAttributeArguments();
        }


        private bool MayRenameType(ElementType type)
        {
            return type == ElementType.Class || type == ElementType.ValueType || type == ElementType.Object || type == ElementType.SZArray;
        }

        private void CollectCArgumentWithTypeOf(IHasCustomAttribute meta, List<CustomAttributeInfo> customAttributes)
        {
            int index = 0;
            foreach (CustomAttribute ca in meta.CustomAttributes)
            {
                List<CAArgument> arguments = null;
                if (ca.ConstructorArguments.Any(a => MayRenameType(a.Type.ElementType)))
                {
                    arguments = ca.ConstructorArguments.ToList();
                }
                List<CANamedArgument> namedArguments = null;
                if (ca.NamedArguments.Any(a => MayRenameType(a.Type.ElementType)))
                {
                    namedArguments = ca.NamedArguments.ToList();
                }
                if (arguments != null | namedArguments != null)
                {
                    customAttributes.Add(new CustomAttributeInfo
                    {
                        customAttributes = meta.CustomAttributes,
                        index = index,
                        arguments = arguments,
                        namedArguments = namedArguments
                    });
                }
                ++index;
            }
        }

        private void BuildCustomAttributeArguments()
        {
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                var customAttributes = new List<CustomAttributeInfo>();
                CollectCArgumentWithTypeOf(ass.module, customAttributes);
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    CollectCArgumentWithTypeOf(type, customAttributes);
                    foreach (FieldDef field in type.Fields)
                    {
                        CollectCArgumentWithTypeOf(field, customAttributes);
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        CollectCArgumentWithTypeOf(method, customAttributes);
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                CollectCArgumentWithTypeOf(param.ParamDef, customAttributes);
                            }
                        }
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        CollectCArgumentWithTypeOf(property, customAttributes);
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        CollectCArgumentWithTypeOf (eventDef, customAttributes);
                    }
                }

                _customAttributeArgumentsWithTypeByMods.Add(ass.module, customAttributes);
            }
        }

        public void Process()
        {
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                if (_renamePolicy.NeedRename(ass.module))
                {
                    Rename(ass.module);
                }
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    if (!IsSystemReservedType(type) && _renamePolicy.NeedRename(type))
                    {
                        Rename(type);
                    }
                    foreach (FieldDef field in type.Fields)
                    {
                        if (_renamePolicy.NeedRename(field))
                        {
                            Rename(field);
                        }
                    }
                    foreach (MethodDef method in type.Methods)
                    {
                        if (_renamePolicy.NeedRename(method))
                        {
                            Rename(method);
                            foreach (Parameter param in method.Parameters)
                            {
                                if (param.ParamDef != null)
                                {
                                    Rename(param.ParamDef);
                                }
                            }
                        }
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        if (_renamePolicy.NeedRename(eventDef))
                        {
                            Rename(eventDef);
                        }
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        if (_renamePolicy.NeedRename(property))
                        {
                            Rename(property);
                        }
                    }
                }
            }
        }

        private bool IsSystemReservedType(TypeDef type)
        {
            if (type.FullName == "<Module>")
            {
                return true;
            }
            return false;
        }

        private List<ObfuzAssemblyInfo> GetReferenceMeAssemblies(ModuleDef mod)
        {
            return _ctx.assemblies.Find(ass => ass.module == mod).referenceMeAssemblies;
        }

        private void Rename(ModuleDefMD mod)
        {
            string oldName = MetaUtil.GetModuleNameWithoutExt(mod.Name);
            string newName = _nameMaker.GetNewName(mod, oldName);
            mod.Name = $"{newName}.dll";
            Debug.Log($"rename module. oldName:{oldName} newName:{newName}");
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                foreach (AssemblyRef assRef in ass.module.GetAssemblyRefs())
                {
                    if (assRef.Name == oldName)
                    {
                        assRef.Name = newName;
                        Debug.Log($"rename assembly:{ass.name}  ref oldName:{oldName} newName:{newName}");
                    }
                }
            }
        }

        private void Rename(TypeDef type)
        {
            string moduleName = MetaUtil.GetModuleNameWithoutExt(type.Module.Name);
            string oldFullName = type.FullName;
            string oldNamespace = type.Namespace;
            string newNamespace;
            if (string.IsNullOrEmpty(oldNamespace))
            {
                newNamespace = oldNamespace;
            }
            else
            {
                newNamespace = _nameMaker.GetNewNamespace(type, oldNamespace);
                type.Namespace = newNamespace;
            }

            string oldName = type.Name;
            string newName = _nameMaker.GetNewName(type, oldName);

            ModuleDefMD mod = (ModuleDefMD)type.Module;
            //RenameTypeRefInCustomAttribute(mod, type, oldFullName, null);
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                RenameTypeRefInCustomAttribute(ass.module, mod, type, oldFullName);
            }

            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                foreach (TypeRef typeRef in ass.module.GetTypeRefs())
                {
                    if (typeRef.FullName != oldFullName)
                    {
                        continue;
                    }
                    if (typeRef.DefinitionAssembly.Name != moduleName)
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(oldNamespace))
                    {
                        typeRef.Namespace = newNamespace;
                    }
                    typeRef.Name = newName;
                    Debug.Log($"rename assembly:{ass.module.Name} reference {oldFullName} => {typeRef.FullName}");
                }
            }
            type.Name = newName;
            string newFullName = type.FullName;
            Debug.Log($"rename typedef. assembly:{type.Module.Name} oldName:{oldFullName} => newName:{newFullName}");
        }


        private TypeSig RenameTypeSig(TypeSig type, ModuleDefMD mod, string oldFullName)
        {
            TypeSig next = type.Next;
            TypeSig newNext = next != null ? RenameTypeSig(next, mod, oldFullName) : null;
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
                    if (vts.DefinitionAssembly.Name != mod.Assembly.Name || vts.TypeDefOrRef.FullName != oldFullName)
                    {
                        return type;
                    }
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
                    ClassOrValueTypeSig newGenericType = (ClassOrValueTypeSig)RenameTypeSig(genericType, mod, oldFullName);
                    bool anyChange = genericType != newGenericType;
                    var genericArgs = new List<TypeSig>();
                    foreach (var arg in gis.GenericArguments)
                    {
                        TypeSig newArg = RenameTypeSig(arg, mod, oldFullName);
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
                    TypeSig newReturnType = RenameTypeSig(methodSig.RetType, mod, oldFullName);
                    bool anyChange = newReturnType != methodSig.RetType;
                    var newArgs = new List<TypeSig>();
                    foreach (TypeSig arg in methodSig.Params)
                    {
                        TypeSig newArg = RenameTypeSig (arg, mod, oldFullName);
                        anyChange |= newArg != newReturnType;
                    }
                    if (!anyChange)
                    {
                        return type;
                    }
                    var newParamsAfterSentinel = new List<TypeSig>();
                    foreach(TypeSig arg in methodSig.ParamsAfterSentinel)
                    {
                        TypeSig newArg = RenameTypeSig(arg, mod, oldFullName);
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


        private object RenameTypeSigOfValue(object oldValue, ModuleDefMD mod, string oldFullName)
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
                return RenameTypeSig(typeSig, mod, oldFullName);
            }
            if (oldValue is CAArgument caValue)
            {
                TypeSig newType = RenameTypeSig(caValue.Type, mod, oldFullName);
                object newValue = RenameTypeSigOfValue(caValue.Value, mod, oldFullName);
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
                    if (TryRenameArgument(mod, oldFullName, oldArg, out var newArg))
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

        private bool TryRenameArgument(ModuleDefMD mod, string oldFullName, CAArgument oldArg, out CAArgument newArg)
        {
            TypeSig newType = RenameTypeSig(oldArg.Type, mod, oldFullName);
            object newValue = RenameTypeSigOfValue(oldArg.Value, mod, oldFullName);
            if (newType != oldArg.Type || oldArg.Value != newValue)
            {
                newArg = new CAArgument(newType, newValue);
                return true;
            }
            newArg = default;
            return false;
        }

        private bool TryRenameArgument(ModuleDefMD mod, string oldFullName, CANamedArgument oldArg)
        {
            bool anyChange = false;
            TypeSig newType = RenameTypeSig(oldArg.Type, mod, oldFullName);
            if (newType != oldArg.Type)
            {
                anyChange = true;
                oldArg.Type = newType;
            }
            if (TryRenameArgument(mod, oldFullName, oldArg.Argument, out var newArg))
            {
                oldArg.Argument = newArg;
                anyChange = true;
            }
            return anyChange;
        }

        private void RenameTypeRefInCustomAttribute(ModuleDefMD referenceMeMod, ModuleDefMD mod, TypeDef typeDef, string oldFullName)
        {
            List<CustomAttributeInfo> customAttributes = _customAttributeArgumentsWithTypeByMods[referenceMeMod];
            foreach (CustomAttributeInfo cai in customAttributes)
            {
                CustomAttribute oldAttr = cai.customAttributes[cai.index];
                bool anyChange = false;
                if (cai.arguments != null)
                {
                    for (int i = 0; i < cai.arguments.Count; i++)
                    {
                        CAArgument oldArg = cai.arguments[i];
                        if (TryRenameArgument(mod, oldFullName, oldArg, out CAArgument newArg))
                        {
                            anyChange = true;
                            cai.arguments[cai.index] = newArg;
                        }
                    }
                }
                if (cai.namedArguments != null)
                {
                    for (int i = 0; i < cai.namedArguments.Count; i++)
                    {
                        CANamedArgument oldArg = cai.namedArguments[i];
                        if (TryRenameArgument(mod, oldFullName, oldArg))
                        {
                            anyChange = true;
                        }
                    }
                }
                if (anyChange)
                {
                    cai.customAttributes[cai.index] = new CustomAttribute(oldAttr.Constructor,
                        cai.arguments != null ? cai.arguments : oldAttr.ConstructorArguments,
                        cai.namedArguments != null ? cai.namedArguments : oldAttr.NamedArguments);
                }
            }
        }

        private void RenameFieldNameInCustomAttributes(ModuleDefMD referenceMeMod, ModuleDefMD mod, string oldFieldOrPropertyName, string newName)
        {
            List<CustomAttributeInfo> customAttributes = _customAttributeArgumentsWithTypeByMods[referenceMeMod];
            foreach (CustomAttributeInfo cai in customAttributes)
            {
                CustomAttribute oldAttr = cai.customAttributes[cai.index];
                bool anyChange = false;
                if (cai.namedArguments != null)
                {
                    foreach (CANamedArgument arg in cai.namedArguments)
                    {
                        if (arg.Name == oldFieldOrPropertyName)
                        {
                            anyChange = true;
                            arg.Name = newName;
                        }
                    }
                }
                if (anyChange)
                {
                    cai.customAttributes[cai.index] = new CustomAttribute(oldAttr.Constructor,
                        cai.arguments != null ? cai.arguments : oldAttr.ConstructorArguments,
                        cai.namedArguments != null ? cai.namedArguments : oldAttr.NamedArguments);
                }
            }
        }

        private void Rename(FieldDef field)
        {
            string oldName = field.Name;
            string newName = _nameMaker.GetNewName(field, oldName);
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(field.DeclaringType.Module))
            {
                foreach (MemberRef memberRef in ass.module.GetMemberRefs())
                {
                    if (!memberRef.IsFieldRef)
                    {
                        continue;
                    }
                    if (oldName != memberRef.Name || !TypeEqualityComparer.Instance.Equals(memberRef.FieldSig.Type, field.FieldSig.Type))
                    {
                        continue;
                    }
                    IMemberRefParent parent = memberRef.Class;
                    if (parent is ITypeDefOrRef typeDefOrRef)
                    {
                        if (typeDefOrRef.IsTypeDef)
                        {
                            if (typeDefOrRef != field.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else if (typeDefOrRef.IsTypeRef)
                        {
                            if (typeDefOrRef.ResolveTypeDefThrow() != field.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else if (typeDefOrRef.IsTypeSpec)
                        {
                            var typeSpec = (TypeSpec)typeDefOrRef;
                            GenericInstSig gis = typeSpec.TryGetGenericInstSig();
                            if (gis == null || gis.GenericType.ToTypeDefOrRef().ResolveTypeDef() != field.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    string oldFieldFullName = memberRef.ToString();
                    memberRef.Name = newName;

                    Debug.Log($"rename assembly:{ass.name} field:{oldFieldFullName} => {memberRef}");
                }

                RenameFieldNameInCustomAttributes(ass.module, (ModuleDefMD)field.DeclaringType.Module, field.Name, newName);
            }
            field.Name = newName;


            Debug.Log($"rename field. {field} => {newName}");
        }

        private void Rename(MethodDef method)
        {
        }

        private void Rename(ParamDef param)
        {
            param.Name = _nameMaker.GetNewName(param, param.Name);
        }

        private void Rename(EventDef eventDef)
        {
            eventDef.Name = _nameMaker.GetNewName(eventDef, eventDef.Name);
        }

        private void Rename(PropertyDef property)
        {
            string oldName = property.Name;
            string newName = _nameMaker.GetNewName(property, oldName);
            property.Name = newName;
            ModuleDefMD mod = (ModuleDefMD)property.DeclaringType.Module;
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                RenameFieldNameInCustomAttributes(ass.module, mod, oldName, newName);
            }
        }
    }
}
