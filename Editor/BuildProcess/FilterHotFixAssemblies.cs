//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEditor;
//using UnityEditor.Build;
//using UnityEngine;

//namespace HybridCLR.Editor.BuildProcessors
//{
//    /// <summary>
//    /// 将热更新dll从Build过程中过滤，防止打包到主工程中
//    /// </summary>
//    internal class FilterHotFixAssemblies : IFilterBuildAssemblies
//    {
//        public int callbackOrder => 0;

//        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
//        {
//                Debug.Log($"[FilterHotFixAssemblies] disabled");
//            return assemblies;
//        }
//    }
//}
