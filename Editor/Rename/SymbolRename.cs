using dnlib.DotNet;
using Obfuz.Rename;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Obfuz
{

    public class SymbolRename
    {
        private readonly ObfuscatorContext _ctx;

        private readonly IRenamePolicy _renamePolicy;

        public SymbolRename(ObfuscatorContext ctx)
        {
            _ctx = ctx;
            _renamePolicy = new RenamePolicy();
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
                                Rename(param.ParamDef);
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
            string newName = _ctx.nameMaker.GetNewName(mod, oldName);
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
                newNamespace = _ctx.nameMaker.GetNewNamespace(type, oldNamespace);
                type.Namespace = newNamespace;
            }

            string oldName = type.Name;
            string newName = _ctx.nameMaker.GetNewName(type, oldName);
            type.Name = newName;
            string newFullName = type.FullName;
            Debug.Log($"rename typedef. assembly:{type.Module.Name} oldName:{oldFullName} => newName:{newFullName}");
            
            foreach (ObfuzAssemblyInfo ass in GetReferenceMeAssemblies(type.Module))
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
        }

        private void Rename(FieldDef field)
        {
            string oldName = field.Name;
            string newName = _ctx.nameMaker.GetNewName(field, oldName);
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
            }
            field.Name = newName;
            Debug.Log($"rename field. {field} => {newName}");
        }

        private void Rename(MethodDef method)
        {
        }

        private void Rename(ParamDef param)
        {

        }

        private void Rename(EventDef eventDef)
        {

        }

        private void Rename(PropertyDef property)
        {
        }
    }
}
