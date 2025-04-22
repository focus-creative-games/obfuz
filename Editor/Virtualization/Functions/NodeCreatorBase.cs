using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public abstract class NodeCreatorBase : IFunction
    {
        void IFunction.Compile(CompileContext ctx, List<IDataNode> inputs, ConstValue result)
        {
            throw new System.NotSupportedException("This function is not supported in this context.");
        }

        public abstract IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options);
    }
}
