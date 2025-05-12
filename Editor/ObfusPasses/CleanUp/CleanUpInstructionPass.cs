using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CleanUp
{
    public class CleanUpInstructionPass : ObfuscationPassBase
    {
        public override ObfuscationPassType Type => ObfuscationPassType.None;

        public override void Start(ObfuscationPassContext ctx)
        {
        }

        public override void Stop(ObfuscationPassContext ctx)
        {

        }

        public override void Process(ObfuscationPassContext ctx)
        {
            foreach (ModuleDef mod in ctx.toObfuscatedModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            CilBody body = method.Body;
                            body.SimplifyBranches();
                            body.OptimizeMacros();
                            body.OptimizeBranches();
                            // TODO remove dup
                        }
                    }
                }
            }
        }
    }
}
