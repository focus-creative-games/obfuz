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
using UnityEngine.Assertions;

namespace Obfuz
{


    public class SymbolRename
    {
        private readonly ObfuscatorContext _ctx;

        private readonly HashSet<ModuleDef> _obfuscatedModules = new HashSet<ModuleDef>();
        private readonly IRenamePolicy _renamePolicy;
        private readonly INameMaker _nameMaker;
        private readonly Dictionary<ModuleDef, List<CustomAttributeInfo>> _customAttributeArgumentsWithTypeByMods = new Dictionary<ModuleDef, List<CustomAttributeInfo>>();
        private readonly RenameRecordMap _renameRecordMap = new RenameRecordMap();
        private readonly VirtualMethodGroupCalculator _virtualMethodGroupCalculator = new VirtualMethodGroupCalculator();

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

            foreach (var mod in ctx.assemblies)
            {
                _obfuscatedModules.Add(mod.module);
            }
            BuildCustomAttributeArguments();
        }

        private void CollectCArgumentWithTypeOf(IHasCustomAttribute meta, List<CustomAttributeInfo> customAttributes)
        {
            int index = 0;
            foreach (CustomAttribute ca in meta.CustomAttributes)
            {
                List<CAArgument> arguments = null;
                if (ca.ConstructorArguments.Any(a => MetaUtil.MayRenameCustomDataType(a.Type.ElementType)))
                {
                    arguments = ca.ConstructorArguments.ToList();
                }
                List<CANamedArgument> namedArguments = null;
                if (ca.NamedArguments.Any(a => MetaUtil.MayRenameCustomDataType(a.Type.ElementType)))
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
            RenameModules();
            RenameTypes();
            RenameFields();
            RenameMethods();
            RenameProperties();
            RenameEvents();
        }

        private List<ObfuzAssemblyInfo> GetReferenceMeAssemblies(ModuleDef mod)
        {
            return _ctx.assemblies.Find(ass => ass.module == mod).referenceMeAssemblies;
        }

        private void RenameModules()
        {
            Debug.Log("Rename Modules begin");
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                if (_renamePolicy.NeedRename(ass.module))
                {
                    Rename(ass.module);
                }
                else
                {
                    _renameRecordMap.AddUnRenameRecord(ass.module);
                }
            }
            Debug.Log("Rename Modules end");
        }


        class RefTypeDefMetas
        {
            public readonly List<TypeRef> typeRefs = new List<TypeRef>();

            public readonly List<CustomAttribute> customAttributes = new List<CustomAttribute>();
        }

        private void BuildRefTypeDefMetasMap(Dictionary<TypeDef, RefTypeDefMetas> refTypeDefMetasMap)
        {
            //foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            //{
            //    RenameTypeRefInCustomAttribute(ass.module, mod, type, oldFullName);
            //}

            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeRef typeRef in ass.module.GetTypeRefs())
                {
                    TypeDef typeDef = typeRef.ResolveThrow();
                    if (!refTypeDefMetasMap.TryGetValue(typeDef, out var typeDefMetas))
                    {
                        typeDefMetas = new RefTypeDefMetas();
                        refTypeDefMetasMap.Add(typeDef, typeDefMetas);
                    }
                    typeDefMetas.typeRefs.Add(typeRef);
                }
            }
        }

        private void RenameTypes()
        {
            Debug.Log("RenameTypes begin");

            var refTypeDefMetasMap = new Dictionary<TypeDef, RefTypeDefMetas>();
            BuildRefTypeDefMetasMap(refTypeDefMetasMap);
            _ctx.assemblyCache.EnableTypeDefCache = false;

            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    if (_renamePolicy.NeedRename(type))
                    {
                        Rename(type, refTypeDefMetasMap.TryGetValue(type, out var typeDefMetas) ? typeDefMetas : null);
                    }
                    else
                    {
                        _renameRecordMap.AddUnRenameRecord(type);
                    }
                }
            }
           _ctx.assemblyCache.EnableTypeDefCache = true;
            Debug.Log("Rename Types end");
        }

        private void RenameFields()
        {
            Debug.Log("Rename fields begin");
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    foreach (FieldDef field in type.Fields)
                    {
                        if (_renamePolicy.NeedRename(field))
                        {
                            Rename(field);
                        }
                        else
                        {
                            _renameRecordMap.AddUnRenameRecord(field);
                        }
                    }
                }
            }
            Debug.Log("Rename fields end");
        }

        private void RenameMethods()
        {
            Debug.Log("Rename methods begin");
            Debug.Log("Rename not virtual methods begin");
            var virtualMethods = new List<MethodDef>();
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    _virtualMethodGroupCalculator.CalculateType(type);
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.IsVirtual)
                        {
                            virtualMethods.Add(method);
                            continue;
                        }
                        if (_renamePolicy.NeedRename(method))
                        {
                            Rename(method);
                        }
                        else
                        {
                            _renameRecordMap.AddUnRenameRecord(method);
                        }
                    }
                }
            }
            Debug.Log("Rename not virtual methods end");


            Debug.Log("Rename virtual methods begin");
            var visitedVirtualMethods = new HashSet<MethodDef>();
            var groupNeedRenames = new Dictionary<VirtualMethodGroup, bool>();
            foreach (var method in virtualMethods)
            {
                if (!visitedVirtualMethods.Add(method))
                {
                    continue;
                }
                VirtualMethodGroup group = _virtualMethodGroupCalculator.GetMethodGroup(method);
                if (!groupNeedRenames.TryGetValue(group, out var needRename))
                {
                    needRename = group.methods.All(m => _obfuscatedModules.Contains(m.DeclaringType.Module) && _renamePolicy.NeedRename(m));
                    groupNeedRenames.Add(group, needRename);
                    if (needRename)
                    {
                        _renameRecordMap.AddRenameRecord(group, method.Name, _nameMaker.GetNewName(method, method.Name));
                    }
                    else
                    {
                        _renameRecordMap.AddUnRenameRecord(group);
                    }
                }
                if (!needRename)
                {
                    _renameRecordMap.AddUnRenameRecord(method);
                    continue;
                }
                if (_renameRecordMap.TryGetRenameRecord(group, out var oldName, out var newName))
                {
                    Rename(method, oldName, newName);
                }
                else
                {
                    throw new Exception($"group:{group} method:{method} not found in rename record map");
                }
            }
            Debug.Log("Rename virtual methods end");
            Debug.Log("Rename methods end");
        }

        private void RenameProperties()
        {
            Debug.Log("Rename properties begin");
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    foreach (PropertyDef property in type.Properties)
                    {
                        if (_renamePolicy.NeedRename(property))
                        {
                            Rename(property);
                        }
                        else
                        {
                            _renameRecordMap.AddUnRenameRecord(property);
                        }
                    }
                }
            }
            Debug.Log("Rename properties end");
        }

        private void RenameEvents()
        {
            Debug.Log("Rename events begin");
            foreach (ObfuzAssemblyInfo ass in _ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    foreach (EventDef eventDef in type.Events)
                    {
                        if (_renamePolicy.NeedRename(eventDef))
                        {
                            Rename(eventDef);
                        }
                        else
                        {
                            _renameRecordMap.AddUnRenameRecord(eventDef);
                        }
                    }
                }
            }
            Debug.Log("Rename events begin");
        }

        private void Rename(ModuleDefMD mod)
        {
            string oldName = MetaUtil.GetModuleNameWithoutExt(mod.Name);
            string newName = _nameMaker.GetNewName(mod, oldName);
            _renameRecordMap.AddRenameRecord(mod, oldName, newName);
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

        private void Rename(TypeDef type, RefTypeDefMetas refTypeDefMeta)
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

            if (refTypeDefMeta != null)
            {
                foreach (TypeRef typeRef in refTypeDefMeta.typeRefs)
                {
                    Assert.AreEqual(typeRef.FullName, oldFullName);
                    Assert.IsTrue(typeRef.DefinitionAssembly.Name == moduleName);
                    if (!string.IsNullOrEmpty(oldNamespace))
                    {
                        typeRef.Namespace = newNamespace;
                    }
                    typeRef.Name = newName;
                    Debug.Log($"rename assembly:{typeRef.Module.Name} reference {oldFullName} => {typeRef.FullName}");
                }
            }
            type.Name = newName;
            string newFullName = type.FullName;
            _renameRecordMap.AddRenameRecord(type, oldFullName, newFullName);
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

        private void RenameFieldNameInCustomAttributes(ModuleDefMD referenceMeMod, ModuleDefMD mod, TypeDef declaringType, string oldFieldOrPropertyName, string newName)
        {
            List<CustomAttributeInfo> customAttributes = _customAttributeArgumentsWithTypeByMods[referenceMeMod];
            foreach (CustomAttributeInfo cai in customAttributes)
            {
                CustomAttribute oldAttr = cai.customAttributes[cai.index];
                if (MetaUtil.GetTypeDefOrGenericTypeBase(oldAttr.Constructor.DeclaringType) != declaringType)
                {
                    continue;
                }
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

                RenameFieldNameInCustomAttributes(ass.module, (ModuleDefMD)field.DeclaringType.Module, field.DeclaringType, field.Name, newName);
            }
            Debug.Log($"rename field. {field} => {newName}");
            field.Name = newName;
            _renameRecordMap.AddRenameRecord(field, oldName, newName);

        }

        private void Rename(MethodDef method)
        {
            string oldName = method.Name;
            string newName = _nameMaker.GetNewName(method, oldName);
            Rename(method, oldName, newName);
        }

        private void Rename(MethodDef method, string oldName, string newName)
        {

            ModuleDefMD mod = (ModuleDefMD)method.DeclaringType.Module;
            RenameMethodParams(method);
            RenameMethodBody(method);
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                foreach (MemberRef memberRef in ass.module.GetMemberRefs())
                {
                    if (!memberRef.IsMethodRef)
                    {
                        continue;
                    }
                    if (oldName != memberRef.Name)
                    {
                        continue;
                    }
                    
                    IMemberRefParent parent = memberRef.Class;
                    if (parent is ITypeDefOrRef typeDefOrRef)
                    {
                        if (typeDefOrRef.IsTypeDef)
                        {
                            if (typeDefOrRef != method.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else if (typeDefOrRef.IsTypeRef)
                        {
                            if (typeDefOrRef.ResolveTypeDefThrow() != method.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else if (typeDefOrRef.IsTypeSpec)
                        {
                            var typeSpec = (TypeSpec)typeDefOrRef;
                            GenericInstSig gis = typeSpec.TryGetGenericInstSig();
                            if (gis == null || gis.GenericType.ToTypeDefOrRef().ResolveTypeDef() != method.DeclaringType)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    // compare methodsig
                    if (!new SigComparer(default).Equals(method.MethodSig, memberRef.MethodSig))
                    {
                        continue;
                    }
                    string oldMethodFullName = memberRef.ToString();
                    memberRef.Name = newName;

                    Debug.Log($"rename assembly:{ass.name} method:{oldMethodFullName} => {memberRef}");
                }
            }

            method.Name = newName;
            _renameRecordMap.AddRenameRecord(method, oldName, newName);

        }

        private void RenameMethodBody(MethodDef method)
        {
            if (method.Body == null)
            {
                return;
            }
        }

        private void RenameMethodParams(MethodDef method)
        {
            foreach (Parameter param in method.Parameters)
            {
                if (param.ParamDef != null)
                {
                    Rename(param.ParamDef);
                }
            }
        }

        private void Rename(ParamDef param)
        {
            // let param name == 1 is more obfuscated
            param.Name = "1";// _nameMaker.GetNewName(param, param.Name);
        }

        private void Rename(EventDef eventDef)
        {
            string oldName = eventDef.Name;
            string newName = _nameMaker.GetNewName(eventDef, eventDef.Name);
            eventDef.Name = newName;
            _renameRecordMap.AddRenameRecord(eventDef, oldName, newName);
        }

        private void Rename(PropertyDef property)
        {
            string oldName = property.Name;
            string newName = _nameMaker.GetNewName(property, oldName);
            ModuleDefMD mod = (ModuleDefMD)property.DeclaringType.Module;
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(mod))
            {
                RenameFieldNameInCustomAttributes(ass.module, mod, property.DeclaringType, oldName, newName);
            }
            property.Name = newName;
            _renameRecordMap.AddRenameRecord(property, oldName, newName);
        }
    }
}
