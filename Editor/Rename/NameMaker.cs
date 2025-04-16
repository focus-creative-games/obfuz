using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{
    public class NameScope
    {
        private readonly List<string> _wordSet;
        private int _nextIndex;

        private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();

        public NameScope(List<string> wordSet)
        {
            _wordSet = wordSet;
            _nextIndex = 0;
        }

        private string CreateNewName()
        {
            var nameBuilder = new StringBuilder();
            for (int i = _nextIndex++; ;)
            {
                nameBuilder.Append(_wordSet[i % _wordSet.Count]);
                i = i / _wordSet.Count;
                if (i == 0)
                {
                    break;
                }
            }
            return nameBuilder.ToString();
        }

        public string GetNewName()
        {
            return CreateNewName();
        }

        public string GetNewName0(string originalName)
        {
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                return newName;
            }
            newName = CreateNewName();
            _nameMap[originalName] = newName;
            return newName;
        }
    }

    public class NameMaker : INameMaker
    {
        private readonly List<string> _wordSet;

        private readonly Dictionary<object, NameScope> _nameScopes = new Dictionary<object, NameScope>();

        private readonly object _namespaceScope = new object();

        public NameMaker(List<string> wordSet)
        {
            _wordSet = wordSet;
        }

        private NameScope GetNameScope(object key)
        {
            if (!_nameScopes.TryGetValue(key, out var nameScope))
            {
                nameScope = new NameScope(_wordSet);
                _nameScopes[key] = nameScope;
            }
            return nameScope;
        }

        public string GetNewName(ModuleDefMD mod, string originalName)
        {
            return GetDefaultNewName(this, originalName);
        }

        private string GetDefaultNewName(object scope, string originName)
        {
            return GetNameScope(scope).GetNewName();
        }

        public string GetNewNamespace(TypeDef typeDef, string originalNamespace)
        {
            if (string.IsNullOrEmpty(originalNamespace))
            {
                return string.Empty;
            }
            return GetNameScope(_namespaceScope).GetNewName0(originalNamespace);
        }

        public string GetNewName(TypeDef typeDef, string originalName)
        {
            return GetDefaultNewName(typeDef.Module, originalName);
        }

        public string GetNewName(MethodDef methodDef, string originalName)
        {
            return GetDefaultNewName(methodDef.DeclaringType, originalName);
        }

        public string GetNewName(ParamDef param, string originalName)
        {
            return "1";
        }

        public string GetNewName(FieldDef fieldDef, string originalName)
        {
            return GetDefaultNewName(fieldDef.DeclaringType, originalName);
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
