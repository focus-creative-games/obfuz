using dnlib.DotNet;

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
}
