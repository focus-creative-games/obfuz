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
using HybridCLR.Editor.MethodBridge;
using System.Linq;
using Analyzer = HybridCLR.Editor.MethodBridge.Analyzer;
using HybridCLR.Editor.Settings;
using Obfuz.Utils;
using FileUtil = Obfuz.Utils.FileUtil;
using IAssemblyResolver = HybridCLR.Editor.Meta.IAssemblyResolver;
using CombinedAssemblyResolver = HybridCLR.Editor.Meta.CombinedAssemblyResolver;
using MetaUtil = HybridCLR.Editor.Meta.MetaUtil;
using AssemblyCache = HybridCLR.Editor.Meta.AssemblyCache;

namespace Obfuz4HybridCLR
{
    public static class PrebuildCommandExt
    {
        public static string GetObfuscatedHotUpdateAssemblyOutputPath(BuildTarget target)
        {
            return $"{ObfuzSettings.Instance.ObfuzRootDir}/{target}/ObfuscatedHotUpdateAssemblies";
        }


        [MenuItem("HybridCLR/ObfuzExtension/GenerateAll")]
        public static void GenerateAll()
        {
            var installer = new InstallerController();
            if (!installer.HasInstalledHybridCLR())
            {
                throw new BuildFailedException($"You have not initialized HybridCLR, please install it via menu 'HybridCLR/Installer'");
            }
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            LinkGeneratorCommand.GenerateLinkXml(target);
            StripAOTDllCommand.GenerateStripedAOTDlls(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);

            string obfuscatedHotUpdateDllPath = GetObfuscatedHotUpdateAssemblyOutputPath(target);
            ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);
            GenerateMethodBridgeAndReversePInvokeWrapper(target, obfuscatedHotUpdateDllPath);
        }

        [MenuItem("HybridCLR/ObfuzExtension/CompileAndObfuscateDll")]
        public static void CompileAndObfuscateDll()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            CompileDllCommand.CompileDll(target);

            string obfuscatedHotUpdateDllPath = GetObfuscatedHotUpdateAssemblyOutputPath(target);
            ObfuscateUtil.ObfuscateHotUpdateAssemblies(target, obfuscatedHotUpdateDllPath);
        }

        public static IAssemblyResolver CreateObfuscatedHotUpdateAssemblyResolver(BuildTarget target, List<string> obfuscatedHotUpdateAssemblies, string obfuscatedHotUpdateDllPath)
        {
            return new FixedSetAssemblyResolver(obfuscatedHotUpdateDllPath, obfuscatedHotUpdateAssemblies);
        }

        public static IAssemblyResolver CreateObfuscatedHotUpdateAndAOTAssemblyResolver(BuildTarget target, List<string> hotUpdateAssemblies, List<string> assembliesToObfuscate, string obfuscatedHotUpdateDllPath)
        {
            return new CombinedAssemblyResolver(
                CreateObfuscatedHotUpdateAssemblyResolver(target, hotUpdateAssemblies.Intersect(assembliesToObfuscate).ToList(), obfuscatedHotUpdateDllPath),
                MetaUtil.CreateHotUpdateAssemblyResolver(target, hotUpdateAssemblies.Except(assembliesToObfuscate).ToList()),
                MetaUtil.CreateAOTAssemblyResolver(target)
                );
        }

        public static void GenerateMethodBridgeAndReversePInvokeWrapper(BuildTarget target, string obfuscatedHotUpdateDllPath)
        {
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            List<string> aotAssemblyNames = Directory.Exists(aotDllDir) ?
                Directory.GetFiles(aotDllDir, "*.dll", SearchOption.TopDirectoryOnly).Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            if (aotAssemblyNames.Count == 0)
            {
                throw new Exception($"no aot assembly found. please run `HybridCLR/Generate/All` or `HybridCLR/Generate/AotDlls` to generate aot dlls before runing `HybridCLR/Generate/MethodBridge`");
            }
            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(MetaUtil.CreateAOTAssemblyResolver(target), aotAssemblyNames);

            var methodBridgeAnalyzer = new Analyzer(new Analyzer.Options
            {
                MaxIterationCount = Math.Min(20, SettingsUtil.HybridCLRSettings.maxMethodBridgeGenericIteration),
                Collector = collector,
            });

            methodBridgeAnalyzer.Run();

            List<string> hotUpdateDlls = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            var cache = new AssemblyCache(CreateObfuscatedHotUpdateAndAOTAssemblyResolver(target, hotUpdateDlls, ObfuzSettings.Instance.assemblySettings.GetAssembliesToObfuscate(), obfuscatedHotUpdateDllPath));

            var reversePInvokeAnalyzer = new MonoPInvokeCallbackAnalyzer(cache, hotUpdateDlls);
            reversePInvokeAnalyzer.Run();

            var calliAnalyzer = new CalliAnalyzer(cache, hotUpdateDlls);
            calliAnalyzer.Run();
            var pinvokeAnalyzer = new PInvokeAnalyzer(cache, hotUpdateDlls);
            pinvokeAnalyzer.Run();
            var callPInvokeMethodSignatures = pinvokeAnalyzer.PInvokeMethodSignatures;

            string templateFile = $"{SettingsUtil.TemplatePathInPackage}/MethodBridge.cpp.tpl";
            string outputFile = $"{SettingsUtil.GeneratedCppDir}/MethodBridge.cpp";

            var callNativeMethodSignatures = calliAnalyzer.CalliMethodSignatures.Concat(pinvokeAnalyzer.PInvokeMethodSignatures).ToList();

            var generateMethodBridgeMethod = typeof(MethodBridgeGeneratorCommand).GetMethod("GenerateMethodBridgeCppFile", BindingFlags.NonPublic | BindingFlags.Static);
            generateMethodBridgeMethod.Invoke(null, new object[] { methodBridgeAnalyzer.GenericMethods, reversePInvokeAnalyzer.ReversePInvokeMethods, callNativeMethodSignatures, templateFile, outputFile });

            MethodBridgeGeneratorCommand.CleanIl2CppBuildCache();
        }
    }
}
