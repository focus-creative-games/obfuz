using dnlib.DotNet;

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

        bool NeedRename(ParamDef paramDef);
    }
}
