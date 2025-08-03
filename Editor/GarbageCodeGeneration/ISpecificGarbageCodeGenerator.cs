using Obfuz.Utils;

namespace Obfuz.GarbageCodeGeneration
{
    public class GenerationParameters
    {
        public IRandom random;

        public string classNamespace;
        public string classNamePrefix;
        public int classCount;
        public int methodCountPerClass;
        public int fieldCountPerClass;
        public string outputPath;
    }

    public interface ISpecificGarbageCodeGenerator
    {
        void Generate(GenerationParameters parameters);
    }
}
