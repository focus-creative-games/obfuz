using dnlib.DotNet;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public abstract class NameMakerBase : INameMaker
    {

        private readonly Dictionary<object, INameScope> _nameScopes = new Dictionary<object, INameScope>();

        private readonly object _namespaceScope = new object();
        private readonly object _typeNameScope = new object();
        private readonly object _methodNameScope = new object();
        private readonly object _fieldNameScope = new object();

        protected abstract INameScope CreateNameScope();

        protected INameScope GetNameScope(object key)
        {
            if (!_nameScopes.TryGetValue(key, out var nameScope))
            {
                nameScope = CreateNameScope();
                _nameScopes[key] = nameScope;
            }
            return nameScope;
        }

        public void AddPreservedName(TypeDef typeDef, string name)
        {
            GetNameScope(_typeNameScope).AddPreservedName(name);
        }

        public void AddPreservedName(MethodDef methodDef, string name)
        {
            GetNameScope(_methodNameScope).AddPreservedName(name);
        }

        public void AddPreservedName(FieldDef fieldDef, string name)
        {
            GetNameScope(_fieldNameScope).AddPreservedName(name);
        }

        public void AddPreservedName(PropertyDef propertyDef, string name)
        {
            GetNameScope(propertyDef.DeclaringType).AddPreservedName(name);
        }

        public void AddPreservedName(EventDef eventDef, string name)
        {
            GetNameScope(eventDef.DeclaringType).AddPreservedName(name);
        }

        public void AddPreservedNamespace(TypeDef typeDef, string name)
        {
            GetNameScope(_namespaceScope).AddPreservedName(name);
        }

        public bool IsNamePreserved(VirtualMethodGroup virtualMethodGroup, string name)
        {
            var scope = GetNameScope(_methodNameScope);
            return scope.IsNamePreserved(name);
        }

        private string GetDefaultNewName(object scope, string originName)
        {
            return GetNameScope(scope).GetNewName(originName, false);
        }

        public string GetNewNamespace(TypeDef typeDef, string originalNamespace, bool reuse)
        {
            if (string.IsNullOrEmpty(originalNamespace))
            {
                return string.Empty;
            }
            return GetNameScope(_namespaceScope).GetNewName(originalNamespace, reuse);
        }

        public string GetNewName(TypeDef typeDef, string originalName)
        {
            return GetDefaultNewName(_typeNameScope, originalName);
        }

        public string GetNewName(MethodDef methodDef, string originalName)
        {
            Assert.IsFalse(methodDef.IsVirtual);
            return GetDefaultNewName(_methodNameScope, originalName);
        }

        public string GetNewName(VirtualMethodGroup virtualMethodGroup, string originalName)
        {
            var scope = GetNameScope(_methodNameScope);
            return scope.GetNewName(originalName, false);
        }

        public virtual string GetNewName(ParamDef param, string originalName)
        {
            return "1";
        }

        public string GetNewName(FieldDef fieldDef, string originalName)
        {
            return GetDefaultNewName(_fieldNameScope, originalName);
        }

        public string GetNewName(PropertyDef propertyDef, string originalName)
        {
            return GetDefaultNewName(propertyDef.DeclaringType, originalName);
        }

        public string GetNewName(EventDef eventDef, string originalName)
        {
            return GetDefaultNewName(eventDef.DeclaringType, originalName);
        }
    }
}
