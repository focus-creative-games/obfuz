using HybridCLR.Editor.Commands;
using HybridCLR.Editor;
using Obfuz.Settings;
using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System;
using System.IO;
using HybridCLR.Editor.Link;
using HybridCLR.Editor.Meta;
using UnityEditor.Build;
using HybridCLR.Editor.Installer;

namespace ObfuzExtension4HybridCLR
{

    public static class PrebuildCommandExt
    {
        [MenuItem("HybridCLR/ObfuzExtension/GenerateAll")]
        public static void GenerateAll()
        {
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            ObfuscateUtil.CompileAndObfuscateHotUpdateAssemblies(target);
            //CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);

            //// obfuscate hot update assemblies
            //string hotUpdateAssemblyOutputPath = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            //string obfuscatedAssemblyOutputPath = ObfuzSettings.Instance.GetObfuscatedAssemblyOutputPath(target);
            //var assemblySearchPaths = new List<string>
            //{
            //    hotUpdateAssemblyOutputPath,
            //};
            //ObfuscateUtil.Obfuscate(target, assemblySearchPaths, obfuscatedAssemblyOutputPath);
            //// override assembly in hot update path with obfuscated assembly
            //foreach (string hotUpdateAssName in SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
            //{
            //    string srcFile = $"{obfuscatedAssemblyOutputPath}/{hotUpdateAssName}.dll";
            //    string dstFile = $"{hotUpdateAssemblyOutputPath}/{hotUpdateAssName}.dll";
            //    // not all assemblies are obfuscated
            //    if (File.Exists(srcFile))
            //    {
            //        File.Copy(srcFile, dstFile, true);
            //        File.SetLastWriteTimeUtc(dstFile, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            //        File.SetCreationTimeUtc(dstFile, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            //        Debug.Log($"[ObfuzExtension] Copy {srcFile} to {dstFile}");
            //    }
            //}

            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            LinkGeneratorCommand.GenerateLinkXml(target);
            StripAOTDllCommand.GenerateStripedAOTDlls(target);
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }
    }
}
