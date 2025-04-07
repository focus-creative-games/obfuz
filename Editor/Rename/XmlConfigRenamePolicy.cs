using dnlib.DotNet;

namespace Obfuz.Rename
{
    public class XmlConfigRenamePolicy : RenamePolicyBase
    {
        public override bool NeedRename(ModuleDefMD mod)
        {
            return false;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            return true;
        }
    }
}
