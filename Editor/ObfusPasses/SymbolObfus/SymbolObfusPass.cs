using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class SymbolObfusPass : ObfuscationPassBase
    {
        private SymbolRename _symbolRename;

        public override ObfuscationPassType Type => ObfuscationPassType.SymbolObfus;

        public SymbolObfusPass(SymbolObfusSettings settings)
        {
            _symbolRename = new SymbolRename(settings);
        }

        public override void Start(ObfuscationPassContext ctx)
        {
            _symbolRename.Init(ctx);
        }

        public override void Stop(ObfuscationPassContext ctx)
        {
            _symbolRename.Save();
        }

        public override void Process(ObfuscationPassContext ctx)
        {
            _symbolRename.Process();
        }
    }
}
