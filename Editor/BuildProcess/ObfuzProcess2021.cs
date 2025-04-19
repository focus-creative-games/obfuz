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
        }

        private static string GetScriptAssembliesPath(object obj)
        {
#if UNITY_2021
            object settings = obj.GetType().GetProperty("settings").GetValue(obj);
            string path = (string)settings.GetType().GetProperty("OutputDirectory").GetValue(settings);
#elif UNITY_2020
            return "Library/PlayerScriptAssemblies";
#else
            throw new Exception();
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
                RunObfuscate(GetScriptAssembliesPath(obj));
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
#elif UNITY_2020
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api/Facades"),
                    Path.Combine(applicationContentsPath, "MonoBleedingEdge/lib/mono/4.7.1-api"),
#else
                    #error "Unsupported Unity version"
#endif
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
