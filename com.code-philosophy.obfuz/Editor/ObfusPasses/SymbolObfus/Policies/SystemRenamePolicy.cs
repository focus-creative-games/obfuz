using dnlib.DotNet;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        private readonly ObfuzIgnoreScopeComputeCache _obfuzIgnoreScopeComputeCache;

        public SystemRenamePolicy(ObfuzIgnoreScopeComputeCache obfuzIgnoreScopeComputeCache)
        {
            _obfuzIgnoreScopeComputeCache = obfuzIgnoreScopeComputeCache;
        }

        private bool IsFullIgnoreObfuscatedType(TypeDef typeDef)
        {
            return typeDef.FullName == "Obfuz.ObfuzIgnoreAttribute" || typeDef.FullName == "Obfuz.ObfuzScope" || typeDef.FullName == "Obfuz.EncryptFieldAttribute";
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

            if (_obfuzIgnoreScopeComputeCache.HasSelfOrEnclosingOrInheritObfuzIgnoreScope(typeDef, ObfuzScope.TypeName))
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

            if (_obfuzIgnoreScopeComputeCache.HasSelfOrInheritPropertyOrEventOrOrTypeDefIgnoreMethodName(methodDef))
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
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(fieldDef, fieldDef.DeclaringType, ObfuzScope.Field))
            {
                return false;
            }
            if (fieldDef.DeclaringType.IsEnum && !fieldDef.IsStatic)
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
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(propertyDef, propertyDef.DeclaringType, ObfuzScope.PropertyName))
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
            if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(eventDef, eventDef.DeclaringType, ObfuzScope.EventName))
            {
                return false;
            }
            return true;
        }
    }
}
