using dnlib.DotNet;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{

    public class UnityRenamePolicy : ObfuscationPolicyBase
    {
        private static HashSet<string> s_monoBehaviourEvents = new HashSet<string> {

            // MonoBehaviour events
    "Awake",
    "FixedUpdate",
    "LateUpdate",
    "OnAnimatorIK",

    "OnAnimatorMove",
    "OnApplicationFocus",
    "OnApplicationPause",
    "OnApplicationQuit",
    "OnAudioFilterRead",

    "OnBecameVisible",
    "OnBecameInvisible",

    "OnCollisionEnter",
    "OnCollisionEnter2D",
    "OnCollisionExit",
    "OnCollisionExit2D",
    "OnCollisionStay",
    "OnCollisionStay2D",
    "OnConnectedToServer",
    "OnControllerColliderHit",

    "OnDrawGizmos",
    "OnDrawGizmosSelected",
    "OnDestroy",
    "OnDisable",
    "OnDisconnectedFromServer",

    "OnEnable",

    "OnFailedToConnect",
    "OnFailedToConnectToMasterServer",

    "OnGUI",

    "OnJointBreak",
    "OnJointBreak2D",

    "OnMasterServerEvent",
    "OnMouseDown",
    "OnMouseDrag",
    "OnMouseEnter",
    "OnMouseExit",
    "OnMouseOver",
    "OnMouseUp",
    "OnMouseUpAsButton",

    "OnNetworkInstantiate",

    "OnParticleSystemStopped",
    "OnParticleTrigger",
    "OnParticleUpdateJobScheduled",
    "OnPlayerConnected",
    "OnPlayerDisconnected",
    "OnPostRender",
    "OnPreCull",
    "OnPreRender",
    "OnRenderImage",
    "OnRenderObject",

    "OnSerializeNetworkView",
    "OnServerInitialized",

    "OnTransformChildrenChanged",
    "OnTransformParentChanged",
    "OnTriggerEnter",
    "OnTriggerEnter2D",
    "OnTriggerExit",
    "OnTriggerExit2D",
    "OnTriggerStay",
    "OnTriggerStay2D",

    "OnValidate",
    "OnWillRenderObject",
    "Reset",
    "Start",
    "Update",

    // Animator/StateMachineBehaviour
    "OnStateEnter",
    "OnStateExit",
    "OnStateMove",
    "OnStateUpdate",
    "OnStateIK",
    "OnStateMachineEnter",
    "OnStateMachineExit",

    // ParticleSystem
    "OnParticleTrigger",
    "OnParticleCollision",
    "OnParticleSystemStopped",

    // UGUI/EventSystems
    "OnPointerClick",
    "OnPointerDown",
    "OnPointerUp",
    "OnPointerEnter",
    "OnPointerExit",
    "OnDrag",
    "OnBeginDrag",
    "OnEndDrag",
    "OnDrop",
    "OnScroll",
    "OnSelect",
    "OnDeselect",
    "OnMove",
    "OnSubmit",
    "OnCancel",
};

        private readonly CachedDictionary<TypeDef, bool> _computeDeclaringTypeDisableAllMemberRenamingCache;
        private readonly CachedDictionary<TypeDef, bool> _isInheritScriptCache;
        private readonly CachedDictionary<TypeDef, bool> _isInheritFromMonoBehaviourCache;
        private readonly CachedDictionary<TypeDef, bool> _isScriptOrSerializableTypeCache;

        public UnityRenamePolicy()
        {
            _computeDeclaringTypeDisableAllMemberRenamingCache = new CachedDictionary<TypeDef, bool>(ComputeDeclaringTypeDisableAllMemberRenaming);
            _isInheritScriptCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsScriptType);
            _isInheritFromMonoBehaviourCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsInheritFromMonoBehaviour);
            _isScriptOrSerializableTypeCache = new CachedDictionary<TypeDef, bool>(MetaUtil.IsScriptOrSerializableType);
        }

        private bool IsUnitySourceGeneratedAssemblyType(TypeDef typeDef)
        {
            if (typeDef.Name.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes_"))
            {
                return true;
            }
            if (typeDef.FullName == "Unity.Entities.CodeGeneratedRegistry.AssemblyTypeRegistry")
            {
                return true;
            }
            if (typeDef.Name.StartsWith("__JobReflectionRegistrationOutput"))
            {
                return true;
            }
            if (MetaUtil.HasDOTSCompilerGeneratedAttribute(typeDef))
            {
                return true;
            }
            if (MetaUtil.HasBurstCompileAttribute(typeDef))
            {
                return true;
            }
            if (typeDef.DeclaringType != null)
            {
                return IsUnitySourceGeneratedAssemblyType(typeDef.DeclaringType);
            }
            return false;
        }

        private bool ComputeDeclaringTypeDisableAllMemberRenaming(TypeDef typeDef)
        {
            if (typeDef.IsEnum && MetaUtil.HasBlackboardEnumAttribute(typeDef))
            {
                return true;
            }
            if (IsUnitySourceGeneratedAssemblyType(typeDef))
            {
                return true;
            }
            if (MetaUtil.IsInheritFromDOTSTypes(typeDef))
            {
                return true;
            }
            return false;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            if (_isScriptOrSerializableTypeCache.GetValue(typeDef))
            {
                return false;
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            if (typeDef.Methods.Any(m => MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute(m)))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            TypeDef typeDef = methodDef.DeclaringType;
            if (s_monoBehaviourEvents.Contains(methodDef.Name) && _isInheritFromMonoBehaviourCache.GetValue(typeDef))
            {
                return false;
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            if (MetaUtil.HasRuntimeInitializeOnLoadMethodAttribute(methodDef))
            {
                return false;
            }
            if (MetaUtil.HasBurstCompileAttribute(methodDef) || MetaUtil.HasDOTSCompilerGeneratedAttribute(methodDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            if (_isScriptOrSerializableTypeCache.GetValue(typeDef))
            {
                if (fieldDef.IsPublic && !fieldDef.IsStatic)
                {
                    return false;
                }
                if (!fieldDef.IsStatic && MetaUtil.IsSerializableField(fieldDef))
                {
                    return false;
                }
            }
            if (_computeDeclaringTypeDisableAllMemberRenamingCache.GetValue(typeDef))
            {
                return false;
            }
            if (MetaUtil.HasBurstCompileAttribute(fieldDef))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            TypeDef typeDef = propertyDef.DeclaringType;
            if (_isScriptOrSerializableTypeCache.GetValue(typeDef))
            {
                bool isGetterPublic = propertyDef.GetMethod != null && propertyDef.GetMethod.IsPublic && !propertyDef.GetMethod.IsStatic;
                bool isSetterPublic = propertyDef.SetMethod != null && propertyDef.SetMethod.IsPublic && !propertyDef.SetMethod.IsStatic;

                if (isGetterPublic || isSetterPublic)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
