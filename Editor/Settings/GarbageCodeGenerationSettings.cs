using System;

namespace Obfuz.Settings
{
    public enum GarbageCodeType
    {
        None,
        Config,
        UI,
    }

    [Serializable]
    public class GarbageCodeGenerationTask
    {
        public int codeGenerationRandomSeed;

        public string classNamespace = "__GarbageCode";

        public string classNamePrefix = "__GeneratedGarbageClass";

        public int classCount = 100;

        public int methodCountPerClass = 10;

        public int fieldCountPerClass = 50;

        public GarbageCodeType garbageCodeType = GarbageCodeType.Config;

        public string outputPath = "Assets/Obfuz/GarbageCode";
    }

    [Serializable]
    public class GarbageCodeGenerationSettings
    {
        public string codeGenerationSecret = "Garbage Code";

        public GarbageCodeGenerationTask defaultTask;

        public GarbageCodeGenerationTask[] additionalTasks;
    }
}
