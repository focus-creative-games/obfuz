using Obfuz.Utils;
using System;
using System.Linq;
using System.Text;

namespace Obfuz.GarbageCodeGeneration
{

    public class UIGarbageCodeGenerator : SpecificGarbageCodeGeneratorBase
    {
        /*
         * 
         *         public Button b1;
        public Image b2;
        public RawImage b30;
        public Text b3;
        public Slider b4;
        public ScrollRect b5;
        public Scrollbar b6;
        public Mask b7;
        public RectMask2D b70;
        public Canvas b8;
        public CanvasGroup b9;
        public RectTransform b10;
        public Transform b11;
        public GameObject b12;
         */

        private readonly string[] _types = new string[]
        {
            "Button",
            "Image",
            "RawImage",
            "Text",
            "Slider",
            "ScrollRect",
            "Scrollbar",
            "Mask",
            "RectMask2D",
            "Canvas",
            "CanvasGroup",
            "RectTransform",
            //"Transform",
            //"GameObject",
        };

        private string CreateRandomType(IRandom random)
        {
            return _types[random.NextInt(_types.Length)];
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

        protected override object CreateField(int index, IRandom random, GenerationParameters parameters)
        {
            return new FieldGenerationInfo
            {
                index = index,
                name = $"x{index}",
                type = CreateRandomType(random),
            };
        }

        protected override object CreateMethod(int index, IRandom random, GenerationParameters parameters)
        {
            return new MethodGenerationInfo
            {
                index = index,
                name = $"Init{index}",
            };
        }

        protected override void GenerateUsings(StringBuilder result, IClassGenerationInfo cgi)
        {
            result.AppendLine("using UnityEngine.UI;");
        }

        protected override void GenerateField(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object field, string indent)
        {
            var fgi = (FieldGenerationInfo)field;
            result.AppendLine($"{indent}public {fgi.type} {fgi.name};");
        }

        protected override void GenerateMethod(StringBuilder result, IClassGenerationInfo cgi, IRandom random, object method, string indent)
        {
            var mgi = (MethodGenerationInfo)method;
            result.AppendLine($"{indent}public void {mgi.name}(GameObject go)");
            result.AppendLine($"{indent}{{");

            string indent2 = indent + "    ";
            result.AppendLine($"{indent2}int a = 0;");
            result.AppendLine($"{indent2}int b = 0;");
            int maxN = 100;
            var shuffledFields = cgi.Fields.ToList();
            RandomUtil.ShuffleList(shuffledFields, random);
            foreach (FieldGenerationInfo fgi in shuffledFields)
            {
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}this.{fgi.name} = go.transform.Find(\"ui/{fgi.name}\").GetComponent<{fgi.type}>();");
                }
                else
                {
                    result.AppendLine($"{indent2}this.{fgi.name} = go.GetComponent<{fgi.type}>();");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}a = b * {random.NextInt(maxN)} + go.layer;");
                    result.AppendLine($"{indent2}b = a * go.layer + {random.NextInt(maxN)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}a *= {random.NextInt(0, 10000)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}b /= {random.NextInt(0, 10000)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}a = a * b << {random.NextInt(0, 10000)};");
                }
                if (random.NextInPercentage(0.5f))
                {
                    result.AppendLine($"{indent2}b = a / b & {random.NextInt(0, 10000)};");
                }
            }

            result.AppendLine($"{indent}}}");
        }
    }
}
