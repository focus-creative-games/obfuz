using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{

    public class UnityRenamePolicy : ObfuscationPolicyBase
    {
        private static HashSet<string> s_monoBehaviourEvents = new HashSet<string> {
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
};
        public override bool NeedRename(TypeDef typeDef)
        {
            if (MetaUtil.IsScriptOrSerializableType(typeDef))
            {
                return false;
            }
            if (typeDef.FullName.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes_"))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            if (MetaUtil.IsInheritFromUnityObject(methodDef.DeclaringType))
            {
                return !s_monoBehaviourEvents.Contains(methodDef.Name);
            }
            if (methodDef.DeclaringType.FullName.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes_"))
            {
                return false;
            }
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            if (MetaUtil.IsScriptOrSerializableType(typeDef))
            {
                return !MetaUtil.IsSerializableField(fieldDef);
            }
            if (fieldDef.DeclaringType.FullName.StartsWith("UnitySourceGeneratedAssemblyMonoScriptTypes_"))
            {
                return false;
            }
            return true;
        }
    }
}
