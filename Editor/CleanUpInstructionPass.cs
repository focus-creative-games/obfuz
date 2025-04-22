using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class CleanUpInstructionPass : ObfuscationPassBase
    {
        public override void Process(ObfuscatorContext ctx)
        {
            // TODO remove all nop instructions
        }

        public override void Start(ObfuscatorContext ctx)
        {
            foreach (var ass in ctx.assemblies)
            {
                foreach (TypeDef type in ass.module.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            CilBody body = method.Body;
                            body.SimplifyBranches();
                            body.OptimizeMacros();
                            body.OptimizeBranches();
                        }
                    }
                }
            }
        }

        public override void Stop(ObfuscatorContext ctx)
        {

        }
    }
}
