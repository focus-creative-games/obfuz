using dnlib.DotNet;
using System.Collections.Generic;

namespace Obfuz.Utils
{
    public class BurstCompileComputeCache
    {
        private readonly List<ModuleDef> _modulesToObfuscate;
        private readonly List<ModuleDef> _allObfuscationRelativeModules;

        private readonly HashSet<MethodDef> _burstCompileMethods = new HashSet<MethodDef>();
        private readonly HashSet<MethodDef> _burstCompileRelativeMethods = new HashSet<MethodDef>();
        public BurstCompileComputeCache(List<ModuleDef> modulesToObfuscate, List<ModuleDef> allObfuscationRelativeModules)
        {
            _modulesToObfuscate = modulesToObfuscate;
            _allObfuscationRelativeModules = allObfuscationRelativeModules;
            Build();
        }


        private void BuildBurstCompileMethods()
        {
            foreach (var module in _allObfuscationRelativeModules)
            {
                foreach (var type in module.GetTypes())
                {
                    bool hasBurstCompileAttribute = MetaUtil.HasBurstCompileAttribute(type);
                    foreach (var method in type.Methods)
                    {
                        if (hasBurstCompileAttribute || MetaUtil.HasBurstCompileAttribute(method))
                        {
                            _burstCompileMethods.Add(method);
                        }
                    }
                }
            }
        }

        private void CollectBurstCompileReferencedMethods()
        {
            var modulesToObfuscateSet = new HashSet<ModuleDef>(_modulesToObfuscate);
            var allObfuscationRelativeModulesSet = new HashSet<ModuleDef>(_allObfuscationRelativeModules);

            var pendingWalking = new Queue<MethodDef>(_burstCompileMethods);
            var visitedMethods = new HashSet<MethodDef>();
            while (pendingWalking.Count > 0)
            {
                var method = pendingWalking.Dequeue();

                if (!visitedMethods.Add(method))
                {
                    continue; // Skip already visited methods
                }
                if (modulesToObfuscateSet.Contains(method.Module))
                {
                    _burstCompileRelativeMethods.Add(method);
                }
                if (!method.HasBody)
                {
                    continue;
                }
                // Check for calls to other methods
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code == dnlib.DotNet.Emit.Code.Call ||
                        instruction.OpCode.Code == dnlib.DotNet.Emit.Code.Callvirt)
                    {
                        MethodDef calledMethod = ((IMethod)instruction.Operand).ResolveMethodDef();
                        if (calledMethod == null || !allObfuscationRelativeModulesSet.Contains(calledMethod.Module) || visitedMethods.Contains(calledMethod))
                        {
                            continue; // Skip if the method could not be resolved
                        }
                        pendingWalking.Enqueue(calledMethod);
                    }
                }
            }
        }

        private void Build()
        {
            BuildBurstCompileMethods();
            CollectBurstCompileReferencedMethods();
        }

        public bool IsBurstCompileMethodOrReferencedByBurstCompileMethod(MethodDef method)
        {
            return _burstCompileRelativeMethods.Contains(method);
        }
    }
}
