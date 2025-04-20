using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class ConstExpression : DataNodeAny
    {
        public IFunction function;
        public readonly List<IDataNode> inputs;
        public readonly ConstValue result;

        public ConstExpression(IFunction function, List<IDataNode> inputs, ConstValue result)
        {
            this.function = function;
            this.inputs = inputs;
            Type = function.ReturnType;
            this.result = result;
        }

        public override void Compile(CompileContext ctx)
        {

        }
    }
}
