using dnlib.DotNet;
using NUnit.Framework;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Obfuz.GarbageCodeGeneration
{
    public abstract class SpecificGarbageCodeGeneratorBase : ISpecificGarbageCodeGenerator
    {
        protected interface IClassGenerationInfo
        {
            string Namespace { get; set; }

            string Name { get; set; }

            IList<object> Fields { get; set; }

            IList<object> Methods { get; set; }
        }

        protected class ClassGenerationInfo : IClassGenerationInfo
        {
            public string Namespace { get; set; }
            public string Name { get; set; }
            public IList<object> Fields { get; set; } = new List<object>();
            public IList<object> Methods { get; set; } = new List<object>();
        }

        public virtual void Generate(GenerationParameters parameters)
        {
            FileUtil.RecreateDir(parameters.outputPath);

            for (int i = 0; i < parameters.classCount; i++)
            {
                Debug.Log($"[{GetType().Name}] Generating class {i}");
                var localRandom = new RandomWithKey(((RandomWithKey)parameters.random).Key, parameters.random.NextInt());
                string outputFile = $"{parameters.outputPath}/__GeneratedGarbageClass_{i}.cs";
                var result = new StringBuilder(64 * 1024);
                GenerateClass(i, localRandom, result, parameters);
                File.WriteAllText(outputFile, result.ToString(), Encoding.UTF8);
                Debug.Log($"[{GetType().Name}] Generated class {i} to {outputFile}");
            }
        }

        protected virtual void GenerateClass(int classIndex, IRandom random, StringBuilder result, GenerationParameters parameters)
        {
            IClassGenerationInfo cgi = CreateClassGenerationInfo(parameters.classNamespace, $"{parameters.classNamePrefix}{classIndex}", random, parameters);
            result.AppendLine("using System;");
            result.AppendLine("using System.Collections.Generic;");
            result.AppendLine("using System.Linq;");
            result.AppendLine("using System.IO;");
            result.AppendLine("using UnityEngine;");

            GenerateUsings(result, cgi);

            result.AppendLine($"namespace {cgi.Namespace}");
            result.AppendLine("{");
            result.AppendLine($"    public class {cgi.Name}");
            result.AppendLine("    {");

            string indent = "        ";
            foreach (object field in cgi.Fields)
            {
                GenerateField(result, cgi, field, indent);
            }
            foreach (object method in cgi.Methods)
            {
                GenerateMethod(result, cgi, method, indent);
            }
            result.AppendLine("    }");
            result.AppendLine("}");
        }

        protected abstract IClassGenerationInfo CreateClassGenerationInfo(string classNamespace, string className, IRandom random, GenerationParameters parameters);

        protected abstract void GenerateUsings(StringBuilder result, IClassGenerationInfo cgi);

        protected abstract void GenerateField(StringBuilder result, IClassGenerationInfo cgi, object field, string indent);

        protected abstract void GenerateMethod(StringBuilder result, IClassGenerationInfo cgi, object method, string indent);
    }

    public class ConfigGarbageCodeGenerator : SpecificGarbageCodeGeneratorBase
    {
        class FieldGenerationInfo
        {
            public int index;
            public string name;
            public string type;
        }

        class MethodGenerationInfo
        {
            public int index;
            public string name;
        }


        protected override IClassGenerationInfo CreateClassGenerationInfo(string classNamespace, string className, IRandom random, GenerationParameters parameters)
        {
            var cgi = new ClassGenerationInfo
            {
                Namespace = classNamespace,
                Name = className,
            };

            for (int i = 0; i < parameters.fieldCountPerClass; i++)
            {
                var fieldInfo = new FieldGenerationInfo
                {
                    index = i,
                    name = $"x{i}",
                    type = CreateRandomType(random),
                };
                cgi.Fields.Add(fieldInfo);
            }

            for (int i = 0; i < parameters.methodCountPerClass; i++)
            {
                var methodInfo = new MethodGenerationInfo
                {
                    index = i,
                    name = $"Load{i}",
                };
                cgi.Methods.Add(methodInfo);
            }

            return cgi;
        }

        private readonly string[] _types = new string[]
        {
            "bool",
            "byte",
            "short",
            "int",
            "long",
            "float",
            "double",
        };

        private string CreateRandomType(IRandom random)
        {
            return _types[random.NextInt(_types.Length)];
        }


        protected override void GenerateUsings(StringBuilder result, IClassGenerationInfo cgi)
        {
        }

        protected override void GenerateField(StringBuilder result, IClassGenerationInfo cgi, object field, string indent)
        {
            var fgi = (FieldGenerationInfo)field;
            result.AppendLine($"{indent}public {fgi.type} {fgi.name};");
        }

        private string GetReadMethodNameOfType(string type)
        {
            switch (type)
            {
                case "bool": return "ReadBoolean";
                case "byte": return "ReadByte";
                case "short": return "ReadInt16";
                case "int": return "ReadInt32";
                case "long": return "ReadInt64";
                case "float": return "ReadSingle";
                case "double": return "ReadDouble";
                default: throw new ArgumentException($"Unsupported type: {type}");
            }
        }

        protected override void GenerateMethod(StringBuilder result, IClassGenerationInfo cgi, object method, string indent)
        {
            result.AppendLine($"{indent}void Load(BinaryReader reader)");
            result.AppendLine($"{indent}{{");

            string indent2 = indent + "    ";
            foreach (FieldGenerationInfo fgi in cgi.Fields)
            {
                result.AppendLine($"{indent2}this.{fgi.name} = reader.{GetReadMethodNameOfType(fgi.type)}();");
            }

            result.AppendLine($"{indent}}}");
        }
    }
}
