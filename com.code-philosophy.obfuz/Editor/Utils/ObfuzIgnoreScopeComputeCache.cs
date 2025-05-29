using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class ObfuzIgnoreScopeComputeCache
    {
        private readonly CachedDictionary<IHasCustomAttribute, ObfuzScope?> _selfObfuzIgnoreScopeCache;
        private readonly CachedDictionary<TypeDef, ObfuzScope?> _enclosingObfuzIgnoreScopeCache;
        private readonly CachedDictionary<TypeDef, ObfuzScope?> _declaringOrEnclosingMemberObfuzIgnoreScopeCache;

        public ObfuzIgnoreScopeComputeCache()
        {
            _selfObfuzIgnoreScopeCache = new CachedDictionary<IHasCustomAttribute, ObfuzScope?>(GetObfuzIgnoreScope);
            _enclosingObfuzIgnoreScopeCache = new CachedDictionary<TypeDef, ObfuzScope?>(GetEnclosingObfuzIgnoreScope);
            _declaringOrEnclosingMemberObfuzIgnoreScopeCache = new CachedDictionary<TypeDef, ObfuzScope?>(GetDeclaringOrEnclosingMemberObfuzIgnoreScope);
        }

        private ObfuzScope? GetObfuzIgnoreScope(IHasCustomAttribute obj)
        {
            var ca = obj.CustomAttributes.FirstOrDefault(c => c.AttributeType.FullName == "Obfuz.ObfuzIgnoreAttribute");
            if (ca == null)
            {
                return null;
            }
            var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
            return scope;
        }

        private ObfuzScope? GetEnclosingObfuzIgnoreScope(TypeDef typeDef)
        {
            TypeDef cur = typeDef.DeclaringType;
            while (cur != null)
            {
                var ca = cur.CustomAttributes?.FirstOrDefault(c => c.AttributeType.FullName == "Obfuz.ObfuzIgnoreAttribute");
                if (ca != null)
                {
                    var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
                    CANamedArgument inheritByNestedTypesArg = ca.GetNamedArgument("ApplyToNestedTypes", false);
                    bool inheritByNestedTypes = inheritByNestedTypesArg == null || (bool)inheritByNestedTypesArg.Value;
                    return inheritByNestedTypes ? (ObfuzScope?)scope : null;
                }
                cur = cur.DeclaringType;
            }
            return null;
        }

        private ObfuzScope? GetDeclaringOrEnclosingMemberObfuzIgnoreScope(TypeDef typeDef)
        {
            TypeDef cur = typeDef;
            while (cur != null)
            {
                var ca = cur.CustomAttributes?.FirstOrDefault(c => c.AttributeType.FullName == "Obfuz.ObfuzIgnoreAttribute");
                if (ca != null)
                {
                    var scope = (ObfuzScope)ca.ConstructorArguments[0].Value;
                    if (cur != typeDef)
                    {
                        CANamedArgument applyToNestedTypesArg = ca.GetNamedArgument("ApplyToNestedTypes", false);
                        if (applyToNestedTypesArg != null && !(bool)applyToNestedTypesArg.Value)
                        {
                            return null;
                        }
                    }
                    return scope;
                }
                cur = cur.DeclaringType;
            }
            return null;
        }

        private bool HasObfuzIgnoreScope(IHasCustomAttribute obj, ObfuzScope targetScope)
        {
            ObfuzScope? objScope = _selfObfuzIgnoreScopeCache.GetValue(obj);
            return objScope != null && (objScope & targetScope) != 0;
        }

        private bool HasEnclosingObfuzIgnoreScope(TypeDef typeDef, ObfuzScope targetScope)
        {
            ObfuzScope? enclosingScope = _enclosingObfuzIgnoreScopeCache.GetValue(typeDef);
            return enclosingScope != null && (enclosingScope & targetScope) != 0;
        }

        private bool HasDeclaringOrEnclosingMemberObfuzIgnoreScope(TypeDef typeDef, ObfuzScope targetScope)
        {
            if (typeDef == null)
            {
                return false;
            }
            ObfuzScope? declaringOrEnclosingScope = _declaringOrEnclosingMemberObfuzIgnoreScopeCache.GetValue(typeDef);
            return declaringOrEnclosingScope != null && (declaringOrEnclosingScope & targetScope) != 0;
        }

        public bool HasSelfOrEnclosingObfuzIgnoreScope(TypeDef typeDef, ObfuzScope targetScope)
        {
            return HasObfuzIgnoreScope(typeDef, targetScope) || HasEnclosingObfuzIgnoreScope(typeDef, targetScope);
        }

        public bool HasSelfOrInheritObfuzIgnoreScope(IHasCustomAttribute obj, TypeDef declaringType, ObfuzScope targetScope)
        {
            return HasObfuzIgnoreScope(obj, targetScope) || HasDeclaringOrEnclosingMemberObfuzIgnoreScope(declaringType, targetScope);
        }

        public bool HasSelfOrInheritPropertyOrEventOrOrTypeDefObfuzIgnoreScope(MethodDef obj, ObfuzScope targetScope)
        {
            if (HasObfuzIgnoreScope(obj, targetScope))
            {
                return true;
            }

            TypeDef declaringType = obj.DeclaringType;

            foreach (var propertyDef in declaringType.Properties)
            {
                if (HasObfuzIgnoreScope(propertyDef, targetScope))
                {
                    return true;
                }
                if (propertyDef.GetMethod == obj || propertyDef.SetMethod == obj)
                {
                    return HasObfuzIgnoreScope(propertyDef, targetScope) || HasDeclaringOrEnclosingMemberObfuzIgnoreScope(declaringType, targetScope);
                }
            }

            foreach (var eventDef in declaringType.Events)
            {
                if (eventDef.AddMethod == obj || eventDef.RemoveMethod == obj)
                {
                    if (HasObfuzIgnoreScope(eventDef, targetScope))
                    {
                        return true;
                    }
                    return HasObfuzIgnoreScope(eventDef, targetScope) || HasDeclaringOrEnclosingMemberObfuzIgnoreScope(declaringType, targetScope);
                }
            }

            return HasDeclaringOrEnclosingMemberObfuzIgnoreScope(declaringType, targetScope);
        }

        public bool HasSelfOrInheritPropertyOrEventOrOrTypeDefIgnoreMethodName(MethodDef obj)
        {
            if (HasObfuzIgnoreScope(obj, ObfuzScope.MethodName))
            {
                return true;
            }

            TypeDef declaringType = obj.DeclaringType;

            foreach (var propertyDef in declaringType.Properties)
            {
                if (propertyDef.GetMethod == obj || propertyDef.SetMethod == obj)
                {
                    return HasSelfOrInheritObfuzIgnoreScope(propertyDef, declaringType, ObfuzScope.PropertyGetterSetterName | ObfuzScope.MethodName);
                }
            }

            foreach (var eventDef in declaringType.Events)
            {
                if (eventDef.AddMethod == obj || eventDef.RemoveMethod == obj)
                {
                    return HasSelfOrInheritObfuzIgnoreScope(eventDef, declaringType, ObfuzScope.EventAddRemoveFireName | ObfuzScope.MethodName);
                }
            }

            return HasDeclaringOrEnclosingMemberObfuzIgnoreScope(declaringType, ObfuzScope.MethodName);
        }
    }
}
