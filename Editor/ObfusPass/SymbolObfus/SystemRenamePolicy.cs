using dnlib.DotNet;

namespace Obfuz.Rename
{
    public class SystemRenamePolicy : RenamePolicyBase
    {
        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>")
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            return methodDef.Name != ".ctor" && methodDef.Name != ".cctor";
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (fieldDef.DeclaringType.IsEnum && fieldDef.Name == "value__")
            {
                return false;
            }
            return true;
        }
    }
}
