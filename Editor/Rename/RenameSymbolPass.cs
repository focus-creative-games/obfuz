using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Rename
{
    public class RenameSymbolPass : ObfuscationPassBase
    {
        private SymbolRename _symbolRename;

        public override void Start(ObfuscatorContext ctx)
        {
            _symbolRename = new SymbolRename(ctx);
        }

        public override void Stop(ObfuscatorContext ctx)
        {
            _symbolRename.Save();
        }

        public override void Process(ObfuscatorContext ctx)
        {
            _symbolRename.Process();
        }
    }
}
