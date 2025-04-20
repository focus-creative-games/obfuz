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

#if UNITY_2019_1_OR_NEWER
    internal class ObfuzProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            s_obfuscated = false;
            FileUtil.RemoveDir(GetScriptAssembliesPath());
        }

        private static string GetScriptAssembliesPath()
        {
#if UNITY_2022_1_OR_NEWER
            return "Library/Bee/PlayerScriptAssemblies";
#else
            return "Library/PlayerScriptAssemblies";
#endif
        }

        private static void OnCompilationFinished(object obj)
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }
            if (!s_obfuscated)
            {
                RunObfuscate(GetScriptAssembliesPath());
                s_obfuscated = true;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            s_obfuscated = false;
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
#if UNITY_2021_1_OR_NEWER
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api/Facades"),
                    Path.Combine(applicationContentsPath, "UnityReferenceAssemblies/unity-4.8-api"),
#elif UNITY_2020 || UNITY_2019
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api/Facades"),
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api"),
#else
#error "Unsupported Unity version"
#endif
                    Path.Combine(applicationContentsPath, "Managed/UnityEngine"),
                   backupPlayerScriptAssembliesPath,
                }.Concat(settings.extraAssemblySearchDirs).ToList(),
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
