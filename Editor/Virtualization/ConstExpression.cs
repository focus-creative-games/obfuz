using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class ConstExpression : DataNodeAny
    {
        public IFunction function;
        public readonly List<IDataNode> Inputs = new List<IDataNode>();

        public override void Compile(CompileContext ctx)
        {

        }
    }
}
