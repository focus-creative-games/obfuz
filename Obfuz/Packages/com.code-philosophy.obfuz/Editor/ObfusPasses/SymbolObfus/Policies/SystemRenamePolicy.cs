using dnlib.DotNet;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>" || name == "ObfuzIgnoreAttribute")
            {
                return false;
            }
            if (MetaUtil.HasObfuzIgnoreAttribute(typeDef))
            {
                return false;
            }
            if (typeDef.DeclaringType != null && MetaUtil.HasObfuzIgnoreAttribute(typeDef.DeclaringType))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (methodDef.DeclaringType.IsDelegate)
            {
                return false;
            }
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
            if (fieldDef.DeclaringType.IsDelegate)
            {
                return false;
            }
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
            if (propertyDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (MetaUtil.HasObfuzIgnoreAttribute(propertyDef) || MetaUtil.HasObfuzIgnoreAttribute(propertyDef.DeclaringType))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (eventDef.DeclaringType.IsDelegate)
            {
                return false;
            }
            if (MetaUtil.HasObfuzIgnoreAttribute(eventDef) || MetaUtil.HasObfuzIgnoreAttribute(eventDef.DeclaringType))
            {
                return false;
            }
            return true;
        }
    }
}
