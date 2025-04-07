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
    }
}
