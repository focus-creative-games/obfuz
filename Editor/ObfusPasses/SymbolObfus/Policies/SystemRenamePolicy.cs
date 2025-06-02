using dnlib.DotNet;
using Obfuz.Editor;
using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{
    public class SystemRenamePolicy : ObfuscationPolicyBase
    {
        private readonly ObfuzIgnoreScopeComputeCache _obfuzIgnoreScopeComputeCache;

        public SystemRenamePolicy(ObfuzIgnoreScopeComputeCache obfuzIgnoreScopeComputeCache)
        {
            _obfuzIgnoreScopeComputeCache = obfuzIgnoreScopeComputeCache;
        }

        private readonly HashSet<string> _fullIgnoreTypeNames = new HashSet<string>
        {
            ConstValues.ObfuzIgnoreAttributeFullName,
            ConstValues.ObfuzScopeFullName,
            ConstValues.EncryptFieldAttributeFullName,
            ConstValues.EmbeddedAttributeFullName,
        };

        private bool IsFullIgnoreObfuscatedType(TypeDef typeDef)
        {
            return _fullIgnoreTypeNames.Contains(typeDef.FullName) || MetaUtil.HasMicrosoftCodeAnalysisEmbeddedAttribute(typeDef);
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
