using dnlib.DotNet;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        private bool IsFullIgnoreObfuscatedType(TypeDef typeDef)
        {
            return typeDef.FullName == "Obfuz.ObfuzIgnoreAttribute" || typeDef.FullName == "Obfuz.ObfuzScope";
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            string name = typeDef.Name;
            if (name == "<Module>")
            {
                return false;
            }
            if (IsFullIgnoreObfuscatedType(typeDef))
            {
                return false;
            }

            if (MetaUtil.HasObfuzIgnoreScope(typeDef, ObfuzScope.TypeName) || MetaUtil.HasEnclosingObfuzIgnoreScope(typeDef.DeclaringType, ObfuzScope.TypeName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (methodDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(methodDef.DeclaringType))
            {
                return false;
            }
            if (methodDef.Name == ".ctor" || methodDef.Name == ".cctor")
            {
                return false;
            }

            if (MetaUtil.HasSelfOrInheritPropertyOrEventOrOrTypeDefIgnoreMethodName(methodDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            if (fieldDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(fieldDef.DeclaringType))
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(fieldDef, fieldDef.DeclaringType, ObfuzScope.Field))
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
            if (propertyDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(propertyDef.DeclaringType))
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(propertyDef, propertyDef.DeclaringType, ObfuzScope.PropertyName))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            if (eventDef.DeclaringType.IsDelegate || IsFullIgnoreObfuscatedType(eventDef.DeclaringType))
            {
                return false;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(eventDef, eventDef.DeclaringType, ObfuzScope.EventName))
            {
                return false;
            }
            return true;
        }
    }
}
