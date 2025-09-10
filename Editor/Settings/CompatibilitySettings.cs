using System;

namespace Obfuz.Settings
{
    public enum RuntimeType
    {
        ActivatedScriptingBackend,
        IL2CPP,
        Mono,
    }

    [Serializable]
    public class CompatibilitySettings
    {
        public RuntimeType targetRuntime;
    }
}
