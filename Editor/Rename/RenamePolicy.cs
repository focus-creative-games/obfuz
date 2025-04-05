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
        bool NeedRename(ModuleDefMD mod);

        bool NeedRename(TypeDef typeDef);

        bool NeedRename(MethodDef methodDef);

        bool NeedRename(FieldDef fieldDef);

        bool NeedRename(PropertyDef propertyDef);

        bool NeedRename(EventDef eventDef);
    }

    public class RenamePolicy : IRenamePolicy
    {
        public bool NeedRename(ModuleDefMD mod)
        {
            return false;
        }

        public bool NeedRename(TypeDef typeDef)
        {
            return true;
        }

        public bool NeedRename(MethodDef methodDef)
        {
            return true;
        }

        public bool NeedRename(FieldDef fieldDef)
        {
            return true;
        }

        public bool NeedRename(PropertyDef propertyDef)
        {
            return true;
        }

        public bool NeedRename(EventDef eventDef)
        {
            return true;
        }
    }
}
