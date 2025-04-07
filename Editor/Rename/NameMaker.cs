using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{
    public interface INameMaker
    {
        string GetNewName(ModuleDefMD mod, string originalName);

        string GetNewName(TypeDef typeDef, string originalName);

        string GetNewNamespace(TypeDef typeDef, string originalNamespace);

        string GetNewName(MethodDef methodDef, string originalName);

        string GetNewName(ParamDef param, string originalName);

        string GetNewName(FieldDef fieldDef, string originalName);

        string GetNewName(PropertyDef propertyDef, string originalName);

        string GetNewName(EventDef eventDef, string originalName);
    }

    public class NameMaker : INameMaker
    {
        private string GetDefaultNewName(string originName)
        {
            return originName + "_xxx__";
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
