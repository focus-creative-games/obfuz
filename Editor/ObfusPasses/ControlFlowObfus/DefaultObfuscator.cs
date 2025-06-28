using dnlib.DotNet;
using UnityEngine;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    class DefaultObfuscator : ObfuscatorBase
    {
        public override bool Obfuscate(MethodDef method, ObfusMethodContext ctx)
        {
            //Debug.Log($"Obfuscating method: {method.FullName} with ControlFlowObfusPass");
            var mcfc = new MethodControlFlowCalculator(method, ctx.CreateRandom(), ctx.constFieldAllocator, ctx.minInstructionCountOfBasicBlockToObfuscate);
            if (!mcfc.TryObfus())
            {
                //Debug.LogWarning($"not obfuscate method: {method.FullName}");
                return false;
            }
            return true;
        }
    }
}
