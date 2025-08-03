using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.CallObfus
{

    public class DelegateProxyObfuscator : ObfuscatorBase
    {
        private readonly GroupByModuleEntityManager _entityManager;

        public DelegateProxyObfuscator(GroupByModuleEntityManager moduleEntityManager)
        {
            _entityManager = moduleEntityManager;
        }

        public override void Done()
        {
            _entityManager.Done<DelegateProxyAllocator>();
        }

        private MethodSig CreateProxyMethodSig(ModuleDef module, IMethod method)
        {
            MethodSig methodSig = MetaUtil.ToSharedMethodSig(module.CorLibTypes, MetaUtil.GetInflatedMethodSig(method, null));
            //MethodSig methodSig = MetaUtil.GetInflatedMethodSig(method).Clone();
            //methodSig.Params
            switch (MetaUtil.GetThisArgType(method))
            {
                case ThisArgType.Class:
                {
                    methodSig.Params.Insert(0, module.CorLibTypes.Object);
                    break;
                }
                case ThisArgType.ValueType:
                {
                    methodSig.Params.Insert(0, module.CorLibTypes.IntPtr);
                    break;
                }
            }
            return MethodSig.CreateStatic(methodSig.RetType, methodSig.Params.ToArray());
        }

        public override bool Obfuscate(MethodDef callingMethod, IMethod calledMethod, bool callVir, List<Instruction> obfuscatedInstructions)
        {
            DelegateProxyAllocator allocator = _entityManager.GetEntity<DelegateProxyAllocator>(callingMethod.Module);
            LocalVariableAllocator localVarAllocator = new LocalVariableAllocator(callingMethod);
            MethodSig methodSig = CreateProxyMethodSig(callingMethod.Module, calledMethod);
            DelegateProxyMethodData proxyData = allocator.Allocate(calledMethod, callVir, methodSig);
            bool isVoidReturn = MetaUtil.IsVoidType(methodSig.RetType);

            using (var varScope = localVarAllocator.CreateScope())
            {
                List<Local> localVars = new List<Local>();
                if (!isVoidReturn)
                {
                    varScope.AllocateLocal(methodSig.RetType);
                }
                foreach (var p in methodSig.Params)
                {
                    localVars.Add(varScope.AllocateLocal(p));
                }
                // save args
                for (int i = localVars.Count - 1; i >= 0; i--)
                {
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Stloc, localVars[i]));
                }
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, proxyData.delegateInstanceField));
                foreach (var local in localVars)
                {
                    obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                }
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Callvirt, proxyData.delegateInvokeMethod));
            }

            return true;
        }
    }
}
