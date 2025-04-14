
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection;
//using UnityEngine;
//using UnityEditor;
//using System.Runtime.CompilerServices;
//using MonoHook;
//using System.IO;
//using Obfuz;

//namespace HybridCLR.MonoHook
//{
//#if UNITY_2021_1_OR_NEWER && !UNITY_2023_1_OR_NEWER
//    [InitializeOnLoad]
//    public class CopyStrippedAOTAssembliesHook2
//    {
//        private static MethodHook _hook;

//        static CopyStrippedAOTAssembliesHook2()
//        {
//            if (_hook == null)
//            {
//                Type type = typeof(UnityEditor.EditorApplication).Assembly.GetType("UnityEditorInternal.Runner");
//                MethodInfo miTarget = type.GetMethod("RunNetCoreProgram", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

//                MethodInfo miReplacement = new StripAssembliesDel(OverrideStripAssembliesTo).Method;
//                MethodInfo miProxy = new StripAssembliesDel(StripAssembliesToProxy).Method;

//                _hook = new MethodHook(miTarget, miReplacement, miProxy);
//                _hook.Install();
//            }
//        }

//        private delegate bool StripAssembliesDel(string exe, string args, string workingDirectory, object parser, object setupStartInfo);

//        private static bool OverrideStripAssembliesTo(string exe, string args, string workingDirectory, object parser, object setupStartInfo)
//        {
//            UnityEngine.Debug.Log($"RunNetCoreProgram");
//            bool result = StripAssembliesToProxy(exe, args, workingDirectory, parser, setupStartInfo);
//            if (!result)
//            {
//                return false;
//            }
//            //UnityEngine.Debug.Log($"== StripAssembliesTo outputDir:{outputFolder}");
//            //string outputStrippedDir = $"Library/Obfuz/StrippedAOTAssemblies/{EditorUserBuildSettings.activeBuildTarget}";
//            //Directory.CreateDirectory(outputStrippedDir);
//            //foreach (var aotDll in Directory.GetFiles(outputFolder, "*.dll"))
//            //{
//            //    string dstFile = $"{outputStrippedDir}/{Path.GetFileName(aotDll)}";
//            //    Debug.Log($"[CopyStrippedAOTAssemblies] copy aot dll {aotDll} -> {dstFile}");
//            //    File.Copy(aotDll, dstFile, true);
//            //}
//            ObfuzProcess.RunObfuscate();
//            return result;
//        }

//        [MethodImpl(MethodImplOptions.NoOptimization)]
//        private static bool StripAssembliesToProxy(string exe, string args, string workingDirectory, object parser, object setupStartInfo)
//        {
//            Debug.LogError("== StripAssembliesToProxy ==");
//            return true;
//        }
//    }
//#endif
//}
