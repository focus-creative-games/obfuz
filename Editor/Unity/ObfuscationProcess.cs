using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.Compilation;
using Obfuz.Utils;
using FileUtil = Obfuz.Utils.FileUtil;
using Obfuz.Settings;

namespace Obfuz.Unity
{

#if UNITY_2019_1_OR_NEWER
    public class ObfuscationProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

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
            string scriptAssembliesPath = GetScriptAssembliesPath();
            if (!s_obfuscated && Directory.Exists(scriptAssembliesPath))
            {
                RunObfuscate(scriptAssembliesPath);
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

            var obfuscatorBuilder = ObfuscatorBuilder.FromObfuzSettings(settings, buildTarget);

            var assemblySearchDirs = new List<string>
                {
                   backupPlayerScriptAssembliesPath,
                };
            obfuscatorBuilder.InsertTopPriorityAssemblySearchDirs(assemblySearchDirs);


            OnObfuscationBegin?.Invoke(new ObfuscationBeginEventArgs
            {
                scriptAssembliesPath = scriptAssembliesPath,
                obfuscatedScriptAssembliesPath = obfuscatorBuilder.ObfuscatedAssemblyOutputDir,
            });
            bool succ = false;

            try
            {
                Obfuscator obfuz = obfuscatorBuilder.Build();
                obfuz.Run();

                foreach (var dllName in settings.assemblySettings.toObfuscatedAssemblyNames.Concat(settings.assemblySettings.notObfuscatedAssemblyNamesReferencingObfuscated))
                {
                    string src = $"{obfuscatorBuilder.ObfuscatedAssemblyOutputDir}/{dllName}.dll";
                    string dst = $"{scriptAssembliesPath}/{dllName}.dll";

                    if (!File.Exists(src))
                    {
                        Debug.LogWarning($"obfuscation assembly not found! skip copy. path:{src}");
                        continue;
                    }
                    File.Copy(src, dst, true);
                    Debug.Log($"obfuscate dll:{dst}");
                }
                succ = true;
            }
            catch (Exception e)
            {
                succ = false;
                Debug.LogException(e);
                Debug.LogError($"Obfuscation failed.");
            }
            OnObfuscationEnd?.Invoke(new ObfuscationEndEventArgs
            {
                success = succ,
                originalScriptAssembliesPath = backupPlayerScriptAssembliesPath,
                obfuscatedScriptAssembliesPath = scriptAssembliesPath,
            });

            Debug.Log("Obfuscation end.");
        }
    }
#endif
}
