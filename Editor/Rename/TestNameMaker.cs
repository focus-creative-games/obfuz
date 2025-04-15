using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{
    public class TestNameMaker : INameMaker
    {
        private int _nextIndex;

        private string GetDefaultNewName(string originName)
        {
            return $"{originName}>{_nextIndex++}";
        }

        public string GetNewName(ModuleDefMD mod, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewNamespace(TypeDef typeDef, string originalNamespace)
        {
            return GetDefaultNewName(originalNamespace);
        }

        public string GetNewName(TypeDef typeDef, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewName(MethodDef methodDef, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewName(ParamDef param, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewName(FieldDef fieldDef, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewName(PropertyDef propertyDef, string originalName)
        {
            return GetDefaultNewName(originalName);
        }

        public string GetNewName(EventDef eventDef, string originalName)
        {
            return GetDefaultNewName(originalName);
        }
    }
}
