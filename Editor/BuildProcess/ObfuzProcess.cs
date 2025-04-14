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
            Debug.Log("Obfuscation begin...");

            string originalPlayerScriptAssembliesPath = @"Library\Bee\PlayerScriptAssemblies";
            string backupPlayerScriptAssembliesPath = @"Library\Bee\PlayerScriptAssemblies_Backup";
            BashUtil.CopyDir(originalPlayerScriptAssembliesPath, backupPlayerScriptAssembliesPath);

            var obfuzedDlls = new List<string> { "Assembly-CSharp" };

            var opt = new Obfuscator.Options
            {
                AssemblySearchDirs = new List<string>
                {
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\MonoBleedingEdge\lib\mono\unityaot-win32\Facades",
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\MonoBleedingEdge\lib\mono\unityaot-win32",
                    @"D:\UnityHubs\2022.3.60f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\il2cpp\Managed",
                   backupPlayerScriptAssembliesPath,
                },
                ObfuscatedAssemblyNames = obfuzedDlls,
                outputDir = $"{backupPlayerScriptAssembliesPath}/obfuzed",
            };
            var obfuz = new Obfuscator(opt);
            obfuz.DoIt();

            foreach (var dllName in obfuzedDlls)
            {
                string src = $"{opt.outputDir}/{dllName}.dll";
                string dst = $"{originalPlayerScriptAssembliesPath}/{dllName}.dll";
                File.Copy(src, dst, true);
            }

            Debug.Log("Obfuscation end.");
        }
    }
}
