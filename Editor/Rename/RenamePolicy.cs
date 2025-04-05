using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{
    public interface IRenamePolicy
    {
        bool NeedKeepName(ModuleDefMD mod);

        bool NeedKeepName(TypeDef typeDef);

        bool NeedKeepName(MethodDef methodDef);

        bool NeedKeepName(FieldDef fieldDef);

        bool NeedKeepName(PropertyDef propertyDef);

        bool NeedKeepName(EventDef eventDef);


        string GetNewName(ModuleDefMD mod, string originalName);

        string GetNewName(TypeDef typeDef, string originalName);

        string GetNewName(MethodDef methodDef, string originalName);

        string GetNewName(FieldDef fieldDef, string originalName);

        string GetNewName(PropertyDef propertyDef, string originalName);

        string GetNewName(EventDef eventDef, string originalName);


    }

    public class RenamePolicy : IRenamePolicy
    {
        public bool NeedKeepName(ModuleDefMD mod)
        {
            return false;
        }
        public bool NeedKeepName(TypeDef typeDef)
        {
            return false;
        }

        public bool NeedKeepName(MethodDef methodDef)
        {
            return false;
        }

        public bool NeedKeepName(FieldDef fieldDef)
        {
            return false;
        }

        public bool NeedKeepName(PropertyDef propertyDef)
        {
            return false;
        }

        public bool NeedKeepName(EventDef eventDef)
        {
            return false;
        }


        public string GetNewName(ModuleDefMD mod, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }

        public string GetNewName(TypeDef typeDef, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }

        public string GetNewName(MethodDef methodDef, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }

        public string GetNewName(FieldDef fieldDef, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }

        public string GetNewName(PropertyDef propertyDef, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }

        public string GetNewName(EventDef eventDef, string originalName)
        {
            return originalName + "_obfuz_generated__";
        }
    }
}
