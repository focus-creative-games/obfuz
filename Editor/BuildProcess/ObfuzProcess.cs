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

namespace Obfuz
{
    internal class ObfuzProcess : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        private static bool s_inBuild = false;
        private static bool s_obfuscated = false;

        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_inBuild = true;
            s_obfuscated = false;
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
            BashUtil.CopyDir(originalPlayerScriptAssembliesPath, backupPlayerScriptAssembliesPath);



            var opt = new Obfuscator.Options
            {
                AssemblySearchDirs = new List<string>
                {
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\MonoBleedingEdge\lib\mono\unityaot-win32\Facades",
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\MonoBleedingEdge\lib\mono\unityaot-win32",
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\il2cpp\Managed",
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
}
