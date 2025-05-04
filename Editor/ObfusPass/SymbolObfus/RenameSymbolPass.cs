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

        public RenameSymbolPass(List<string> obfuscationRuleFiles, string mappingXmlPath)
        {
            _symbolRename = new SymbolRename(mappingXmlPath, obfuscationRuleFiles);
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
