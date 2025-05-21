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

namespace Obfuz4HybridCLR
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
            Il2CppDefGeneratorCommand.GenerateIl2CppDef();
            LinkGeneratorCommand.GenerateLinkXml(target);
            StripAOTDllCommand.GenerateStripedAOTDlls(target);
            MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target);
            AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target);
        }

        [MenuItem("HybridCLR/ObfuzExtension/CompileAndObfuscateDll")]
        public static void CompileAndObfuscateDll()
        {
            ObfuscateUtil.CompileAndObfuscateHotUpdateAssemblies(EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
