using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using Obfuz.Settings;
using Obfuz4HybridCLR;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildCommand
{
    [MenuItem("Build/CompileAndObfuscateAndCopyToStreamingAssets")]
    public static void CompileAndObfuscateAndCopyToStreamingAssets()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        CompileDllCommand.CompileDll(target);

        string obfuscatedHotUpdateDllPath = PrebuildCommandExt.GetObfuscatedHotUpdateAssemblyOutputPath(target);
        ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);

        Directory.CreateDirectory(Application.streamingAssetsPath);

        string hotUpdateDllPath = $"{SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target)}";
        List<string> obfuscationRelativeAssemblyNames = ObfuzSettings.Instance.assemblySettings.GetObfuscationRelativeAssemblyNames();

        foreach (string assName in SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
        {
            string srcDir = obfuscationRelativeAssemblyNames.Contains(assName) ? obfuscatedHotUpdateDllPath : hotUpdateDllPath;
            string srcFile = $"{srcDir}/{assName}.dll";
            string dstFile = $"{Application.streamingAssetsPath}/{assName}.dll.bytes";
            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, dstFile, true);
                Debug.Log($"[CompileAndObfuscate] Copy {srcFile} to {dstFile}");
            }
        }
    }
}
