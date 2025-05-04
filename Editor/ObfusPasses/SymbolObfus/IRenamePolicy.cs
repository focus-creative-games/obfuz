using dnlib.DotNet;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public interface IRenamePolicy
    {
        bool NeedRename(ModuleDef mod);

        bool NeedRename(TypeDef typeDef);

        bool NeedRename(MethodDef methodDef);

        bool NeedRename(FieldDef fieldDef);

        bool NeedRename(PropertyDef propertyDef);

        bool NeedRename(EventDef eventDef);

        bool NeedRename(ParamDef paramDef);
    }
}
