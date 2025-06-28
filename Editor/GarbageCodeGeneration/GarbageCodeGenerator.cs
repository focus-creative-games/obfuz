using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Obfuz.GarbageCodeGeneration
{

    public class GarbageCodeGenerator
    {
        private const int CodeGenerationSecretKeyLength = 1024;

        private readonly GarbageCodeGenerationSettings _settings;
        private readonly int[] _intGenerationSecretKey;

        public GarbageCodeGenerator(GarbageCodeGenerationSettings settings)
        {
            _settings = settings;

            byte[] byteGenerationSecretKey = KeyGenerator.GenerateKey(settings.codeGenerationSecret, CodeGenerationSecretKeyLength);
            _intGenerationSecretKey = KeyGenerator.ConvertToIntKey(byteGenerationSecretKey);
        }

        public void Generate()
        {
            GenerateTask(_settings.defaultTask);
            if (_settings.additionalTasks != null && _settings.additionalTasks.Length > 0)
            {
                foreach (var task in _settings.additionalTasks)
                {
                    GenerateTask(task);
                }
            }
        }

        public void CleanCodes()
        {
            Debug.Log($"Cleaning generated garbage codes begin.");
            if (_settings.defaultTask != null)
            {
                FileUtil.RemoveDir(_settings.defaultTask.outputPath, true);
            }
            if (_settings.additionalTasks != null && _settings.additionalTasks.Length > 0)
            {
                foreach (var task in _settings.additionalTasks)
                {
                    FileUtil.RemoveDir(task.outputPath, true);
                }
            }
        }

        private void GenerateTask(GarbageCodeGenerationTask task)
        {
            Debug.Log($"Generating garbage code with seed: {task.codeGenerationRandomSeed}, class count: {task.classCount}, method count per class: {task.methodCountPerClass}, types: {task.garbageCodeType}, output path: {task.outputPath}");

            if (string.IsNullOrWhiteSpace(task.outputPath))
            {
                throw new Exception("outputPath of GarbageCodeGenerationTask is empty!");
            }

            var generator = CreateSpecificCodeGenerator(task.garbageCodeType);

            var parameters = new GenerationParameters
            {
                random = new RandomWithKey(_intGenerationSecretKey, task.codeGenerationRandomSeed),
                classNamespace = task.classNamespace,
                classNamePrefix = task.classNamePrefix,
                classCount = task.classCount,
                methodCountPerClass = task.methodCountPerClass,
                fieldCountPerClass = task.fieldCountPerClass,
                outputPath = task.outputPath,
            };
            generator.Generate(parameters);

            Debug.Log($"Generate garbage code end.");
        }

        private ISpecificGarbageCodeGenerator CreateSpecificCodeGenerator(GarbageCodeType type)
        {
            switch (type)
            {
                case GarbageCodeType.Config: return new ConfigGarbageCodeGenerator();
                case GarbageCodeType.UI: return new UIGarbageCodeGenerator();
                default: throw new NotSupportedException($"Garbage code type {type} is not supported.");
            }
        }
    }
}
