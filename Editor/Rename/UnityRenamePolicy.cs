using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{

    public class UnityRenamePolicy : RenamePolicyBase
    {
        private static HashSet<string> s_monoBehaviourEvents = new HashSet<string> {
    "Awake",
    "OnEnable",
    "Start",
    "FixedUpdate",
    "Update",
    "LateUpdate",
    "OnDisable",
    "OnDestroy",
    "OnApplicationQuit",
    "OnTriggerEnter",
    "OnTriggerExit",
    "OnTriggerStay",
    "OnCollisionEnter",
    "OnCollisionExit",
    "OnCollisionStay",
    "OnMouseDown",
    "OnMouseUp",
    "OnMouseEnter",
    "OnMouseExit",
    "OnMouseOver",
    "OnMouseDrag",
    "OnBecameVisible",
    "OnBecameInvisible",
    "OnGUI",
    "OnPreRender",
    "OnPostRender",
    "OnRenderObject",
    "OnDrawGizmos",
    "OnDrawGizmosSelected",
    "OnValidate",
    "OnAnimatorIK",
    "OnAnimatorMove",
    "OnApplicationFocus",
    "OnApplicationPause",
    "OnAudioFilterRead",
    "OnJointBreak",
    "OnParticleCollision",
    "OnTransformChildrenChanged",
    "OnTransformParentChanged",
    "OnRectTransformDimensionsChange",
    "OnWillRenderObject"
};
        public override bool NeedRename(TypeDef typeDef)
        {
            if (MetaUtil.IsScriptOrSerializableType(typeDef))
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
            return true;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            if (MetaUtil.IsScriptOrSerializableType(typeDef))
            {
                return !MetaUtil.IsSerializableField(fieldDef);
            }
            return true;
        }
    }
}
