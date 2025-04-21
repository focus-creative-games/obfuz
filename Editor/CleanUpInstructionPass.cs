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

        }

        public override void Stop(ObfuscatorContext ctx)
        {

        }
    }
}
