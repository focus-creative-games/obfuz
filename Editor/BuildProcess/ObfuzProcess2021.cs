using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEditor.Compilation;
using System.Reflection;

namespace Obfuz
{

#if UNITY_2021 || UNITY_2020_1_OR_NEWER
    internal class ObfuzProcess2021 : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool s_inBuild = false;
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_inBuild = true;
            s_obfuscated = false;

            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static string GetScriptAssembliesPath(object obj)
        {
            object settings = obj.GetType().GetProperty("settings").GetValue(obj);
            string path = (string)settings.GetType().GetProperty("OutputDirectory").GetValue(settings);
            return path;
        }

        private void OnCompilationFinished(object obj)
        {
            if (s_inBuild && !s_obfuscated)
            {
                RunObfuscate(GetScriptAssembliesPath(obj));
                s_obfuscated = true;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            s_inBuild = false;
            s_obfuscated = false;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private static void RunObfuscate(string scriptAssembliesPath)
        {
            ObfuzSettings settings = ObfuzSettings.Instance;
            if (!settings.enable)
            {
                Debug.Log("Obfuscation is disabled.");
                return;
            }

            Debug.Log("Obfuscation begin...");
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;


            string backupPlayerScriptAssembliesPath = settings.GetOriginalAssemblyBackupDir(buildTarget);
            FileUtil.CopyDir(scriptAssembliesPath, backupPlayerScriptAssembliesPath);

            string applicationContentsPath = EditorApplication.applicationContentsPath;

            var opt = new Obfuscator.Options
            {
                AssemblySearchDirs = new List<string>
                {
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api/Facades"),
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api"),
                    Path.Combine(applicationContentsPath, "Managed/UnityEngine"),
                   backupPlayerScriptAssembliesPath,
                },
                ObfuscationRuleFiles = settings.ruleFiles.ToList(),
                mappingXmlPath = settings.mappingFile,
                outputDir = ObfuzSettings.Instance.GetObfuscatedAssemblyOutputDir(buildTarget),
            };
            var obfuz = new Obfuscator(opt);
            obfuz.Run();

            foreach (var dllName in obfuz.ObfuscatedAssemblyNames)
            {
                string src = $"{opt.outputDir}/{dllName}.dll";
                string dst = $"{scriptAssembliesPath}/{dllName}.dll";
                File.Copy(src, dst, true);
                Debug.Log($"obfuscate dll:{dst}");
            }

            Debug.Log("Obfuscation end.");
        }
    }
#endif
}
