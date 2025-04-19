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

namespace Obfuz
{

#if DISABLE
    internal class ObfuzProcess2022OrNewer : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        private static bool s_inBuild = false;
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_inBuild = true;
            s_obfuscated = false;

            //CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private void OnCompilationFinished(object obj)
        {
            if (s_inBuild && !s_obfuscated)
            {
                RunObfuscate();
                s_obfuscated = true;
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (s_inBuild && !s_obfuscated)
            {
                RunObfuscate();
                s_obfuscated = true;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            s_inBuild = false;
            s_obfuscated = false;
            //CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private static void RunObfuscate()
        {
            ObfuzSettings settings = ObfuzSettings.Instance;
            if (!settings.enable)
            {
                Debug.Log("Obfuscation is disabled.");
                return;
            }

            Debug.Log("Obfuscation begin...");
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;


            string originalPlayerScriptAssembliesPath = @"Library\Bee\PlayerScriptAssemblies";
            string backupPlayerScriptAssembliesPath = settings.GetOriginalAssemblyBackupDir(buildTarget);
            FileUtil.CopyDir(originalPlayerScriptAssembliesPath, backupPlayerScriptAssembliesPath);

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
                string dst = $"{originalPlayerScriptAssembliesPath}/{dllName}.dll";
                File.Copy(src, dst, true);
                Debug.Log($"obfuscate dll:{dst}");
            }

            Debug.Log("Obfuscation end.");
        }
    }
#endif
}
