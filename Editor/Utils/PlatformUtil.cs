using UnityEditor;

namespace Obfuz.Utils
{
    public static class PlatformUtil
    {
        public static bool IsMonoBackend()
        {
            return PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup)
                == ScriptingImplementation.Mono2x;
        }
    }
}
