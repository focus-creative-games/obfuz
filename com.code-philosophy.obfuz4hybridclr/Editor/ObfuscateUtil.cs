using HybridCLR.Editor;
using Obfuz.Settings;
using Obfuz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using System.IO;
using HybridCLR.Editor.ABI;
using UnityEngine;
using Obfuz.Unity;

namespace Obfuz4HybridCLR
{
    public static class ObfuscateUtil
    {
        public static bool AreSameDirectory(string path1, string path2)
        {
            try
            {
                var dir1 = new DirectoryInfo(path1);
                var dir2 = new DirectoryInfo(path2);

                // 比较完整路径（考虑符号链接）
                return dir1.FullName.TrimEnd('\\') == dir2.FullName.TrimEnd('\\');
            }
            catch
            {
                return false;
            }
        }

        public static void CompileAndObfuscateHotUpdateAssemblies(BuildTarget target)
        {
            string hotUpdateDllPath = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            BashUtil.RemoveDir(hotUpdateDllPath);
            CompileDllCommand.CompileDll(target);

            AssemblySettings assemblySettings = ObfuzSettings.Instance.assemblySettings;
            ObfuscationProcess.ValidateReferences(hotUpdateDllPath, new HashSet<string>(assemblySettings.GetAssembliesToObfuscate()), new HashSet<string>(assemblySettings.GetObfuscationRelativeAssemblyNames()));
            var assemblySearchPaths = new List<string>
            {
                hotUpdateDllPath,
            };
            Obfuscate(target, assemblySearchPaths, hotUpdateDllPath);
        }

        public static void Obfuscate(BuildTarget target, List<string> assemblySearchPaths, string outputPath)
        {
            var obfuzSettings = ObfuzSettings.Instance;

            var assemblySearchDirs = assemblySearchPaths;
            ObfuscatorBuilder builder = ObfuscatorBuilder.FromObfuzSettings(obfuzSettings, target, true);
            builder.InsertTopPriorityAssemblySearchPaths(assemblySearchDirs);

            string obfuscatedAssemblyOutputPath = obfuzSettings.GetObfuscatedAssemblyOutputPath(target);
            if (AreSameDirectory(outputPath, obfuscatedAssemblyOutputPath))
            {
                throw new Exception($"outputPath:{outputPath} can't be same to ObfuscatedAssemblyOutputPath:{obfuscatedAssemblyOutputPath}");
            }
            foreach (var assemblySearchDir in builder.CoreSettingsFacade.assemblySearchPaths)
            {
                if (AreSameDirectory(assemblySearchDir, obfuscatedAssemblyOutputPath))
                {
                    throw new Exception($"assemblySearchDir:{assemblySearchDir} can't be same to ObfuscatedAssemblyOutputPath:{obfuscatedAssemblyOutputPath}");
                }
            }

            Obfuscator obfuz = builder.Build();
            obfuz.Run();

            Directory.CreateDirectory(outputPath);
            foreach (string srcFile in Directory.GetFiles(obfuscatedAssemblyOutputPath, "*.dll"))
            {
                string fileName = Path.GetFileName(srcFile);
                string destFile = $"{outputPath}/{fileName}";
                File.Copy(srcFile, destFile, true);
                Debug.Log($"Copy {srcFile} to {destFile}");
            }
        }
    }
}
