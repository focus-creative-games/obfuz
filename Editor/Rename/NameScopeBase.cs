using System.Collections.Generic;
using System.Text;

namespace Obfuz.Rename
{
    public abstract class NameScopeBase : INameScope
    {

        private readonly Dictionary<string, string> _nameMap = new Dictionary<string, string>();

        private readonly HashSet<string> _preservedNames = new HashSet<string>();


        public void AddPreservedName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _preservedNames.Add(name);
            }
        }


        protected abstract void BuildNewName(StringBuilder nameBuilder, string originalName);

        private string CreateNewName(string originalName)
        {
            var nameBuilder = new StringBuilder();
            while (true)
            {
                nameBuilder.Clear();
                BuildNewName(nameBuilder, originalName);
                string newName = nameBuilder.ToString();
                if (_preservedNames.Add(newName))
                {
                    return newName;
                }
            }
        }

        public string GetNewName(string originalName, bool reuse)
        {
            if (!reuse)
            {
                return CreateNewName(originalName);
            }
            if (_nameMap.TryGetValue(originalName, out var newName))
            {
                return newName;
            }
            newName = CreateNewName(originalName);
            _nameMap[originalName] = newName;
            return newName;
        }
    }
}
