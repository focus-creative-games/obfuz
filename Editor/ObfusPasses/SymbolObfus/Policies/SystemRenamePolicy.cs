using dnlib.DotNet;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>")
            {
                return false;
            }
            if (MetaUtil.HasObfuzIgnoreAttribute(typeDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (methodDef.Name == ".ctor" || methodDef.Name == ".cctor")
            {
                return false;
            }

            if (MetaUtil.HasObfuzIgnoreAttribute(methodDef) || MetaUtil.HasObfuzIgnoreAttribute(methodDef.DeclaringType))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (MetaUtil.HasObfuzIgnoreAttribute(fieldDef) || MetaUtil.HasObfuzIgnoreAttribute(fieldDef.DeclaringType))
            {
                return false;
            }
            if (fieldDef.DeclaringType.IsEnum && fieldDef.Name == "value__")
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            if (MetaUtil.HasObfuzIgnoreAttribute(propertyDef) || MetaUtil.HasObfuzIgnoreAttribute(propertyDef.DeclaringType))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (MetaUtil.HasObfuzIgnoreAttribute(eventDef) || MetaUtil.HasObfuzIgnoreAttribute(eventDef.DeclaringType))
            {
                return false;
            }
            return true;
        }
    }
}
