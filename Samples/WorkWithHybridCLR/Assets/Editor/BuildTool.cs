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

public class BuildTool : MonoBehaviour
{
    [MenuItem("Obfuz/GenerateLinkXmlForHybridCLR")]
    public static void GenerateLinkXml()
    {
        CompileDllCommand.CompileDllActiveBuildTarget();
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        var obfuzSettings = ObfuzSettings.Instance;

        var assemblySearchDirs = new List<string>
        {
            SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target),
        };
        ObfuscatorBuilder builder = ObfuscatorBuilder.FromObfuzSettings(obfuzSettings, target, true);
        builder.InsertTopPriorityAssemblySearchPaths(assemblySearchDirs);

        Obfuscator obfuz = builder.Build();
        obfuz.Run();


        List<string> hotfixAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

        var analyzer = new Analyzer(new PathAssemblyResolver(builder.ObfuscatedAssemblyOutputPath));
        var refTypes = analyzer.CollectRefs(hotfixAssemblies);

        // HyridCLR中 LinkXmlWritter不是public的，在其他程序集无法访问，只能通过反射操作
        var linkXmlWriter = typeof(SettingsUtil).Assembly.GetType("HybridCLR.Editor.Link.LinkXmlWriter");
        var writeMethod = linkXmlWriter.GetMethod("Write", BindingFlags.Public | BindingFlags.Instance);
        var instance = Activator.CreateInstance(linkXmlWriter);
        string linkXmlOutputPath = $"{Application.dataPath}/Obfuz/link.xml";
        writeMethod.Invoke(instance, new object[] { linkXmlOutputPath, refTypes });
        Debug.Log($"[GenerateLinkXmlForObfuscatedAssembly] output:{linkXmlOutputPath}");
        AssetDatabase.Refresh();
    }

    [MenuItem("Obfuz/CompileAndObfuscateAndCopyToStreamingAssets")]
    public static void CompileAndObfuscateAndCopyToStreamingAssets()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outputPath = ObfuzSettings.Instance.GetObfuscatedAssemblyOutputPath(target);
        CompileAndObfuscate(target, outputPath);

        Directory.CreateDirectory(Application.streamingAssetsPath);

        foreach (string assName in SettingsUtil.HotUpdateAssemblyNamesIncludePreserved)
        {
            string srcFile = $"{outputPath}/{assName}.dll";
            string dstFile = $"{Application.streamingAssetsPath}/{assName}.dll.bytes";
            File.Copy(srcFile, dstFile, true);
            Debug.Log($"[CompileAndObfuscate] Copy {srcFile} to {dstFile}");
        }
    }


    public static void CompileAndObfuscate(BuildTarget target, string outputPath)
    {
        CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.development);
        var assemblySearchPaths = new List<string>
      {
        SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target),
      };
        CustomObfuscate(target, assemblySearchPaths, outputPath);
    }

    public static void CustomObfuscate(BuildTarget target, List<string> assemblySearchPaths, string outputPath)
    {
        var obfuzSettings = ObfuzSettings.Instance;

        var assemblySearchDirs = assemblySearchPaths;
        ObfuscatorBuilder builder = ObfuscatorBuilder.FromObfuzSettings(obfuzSettings, target, true);
        builder.InsertTopPriorityAssemblySearchPaths(assemblySearchDirs);
        builder.ObfuscatedAssemblyOutputPath = outputPath;

        Obfuscator obfuz = builder.Build();
        obfuz.Run();
    }
}
