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
    public class ObfuzProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

        public class ObfuscationBeginEventArgs : EventArgs
        {
            public string scriptAssembliesPath;
            public string obfuscatedScriptAssembliesPath;
        }

        public class ObfuscationEndEventArgs : EventArgs
        {
            public string originalScriptAssembliesPath;
            public string obfuscatedScriptAssembliesPath;
        }

        public static event Action<ObfuscationBeginEventArgs> OnObfuscationBegin;

        public static event Action<ObfuscationEndEventArgs> OnObfuscationEnd;

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
            string obfuscatedAssemblyOutputDir = settings.GetObfuscatedAssemblyOutputDir(buildTarget);
            OnObfuscationBegin?.Invoke(new ObfuscationBeginEventArgs
            {
                scriptAssembliesPath = scriptAssembliesPath,
                obfuscatedScriptAssembliesPath = obfuscatedAssemblyOutputDir,
            });

            string backupPlayerScriptAssembliesPath = settings.GetOriginalAssemblyBackupDir(buildTarget);
            FileUtil.CopyDir(scriptAssembliesPath, backupPlayerScriptAssembliesPath);

            string applicationContentsPath = EditorApplication.applicationContentsPath;

            var opt = new Obfuscator.Options
            {
                obfuscationAssemblyNames = settings.obfuscationAssemblyNames.ToList(),
                assemblySearchDirs = new List<string>
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
                obfuscationRuleFiles = settings.ruleFiles.ToList(),
                mappingXmlPath = settings.mappingFile,
                outputDir = obfuscatedAssemblyOutputDir,
            };
            var obfuz = new Obfuscator(opt);
            obfuz.Run();

            foreach (var dllName in settings.obfuscationAssemblyNames)
            {
                string src = $"{opt.outputDir}/{dllName}.dll";
                string dst = $"{scriptAssembliesPath}/{dllName}.dll";

                if (!File.Exists(src))
                {
                    Debug.LogWarning($"obfuscation assembly not found! skip copy. path:{src}");
                    continue;
                }
                File.Copy(src, dst, true);
                Debug.Log($"obfuscate dll:{dst}");
            }
            OnObfuscationEnd?.Invoke(new ObfuscationEndEventArgs
            {
                originalScriptAssembliesPath = backupPlayerScriptAssembliesPath,
                obfuscatedScriptAssembliesPath = scriptAssembliesPath,
            });

            Debug.Log("Obfuscation end.");
        }
    }
#endif
        }
