using dnlib.DotNet;

namespace Obfuz.Rename
{
    public abstract class RenamePolicyBase : IRenamePolicy
    {
        public virtual bool NeedRename(ModuleDefMD mod)
        {
            return true;
        }

        public virtual bool NeedRename(TypeDef typeDef)
        {
            return true;
        }

        public virtual bool NeedRename(MethodDef methodDef)
        {
            return true;
        }

        public virtual bool NeedRename(FieldDef fieldDef)
        {
            return true;
        }

        public virtual bool NeedRename(PropertyDef propertyDef)
        {
            return true;
        }

        public virtual bool NeedRename(EventDef eventDef)
        {
            return true;
        }

        public virtual bool NeedRename(ParamDef paramDef)
        {
            return true;
        }
    }
}
