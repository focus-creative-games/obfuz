using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public interface IFunction
    {
        DataNodeType ReturnType { get; }

        ConstExpression CreateExpr(DataNodeType type, object value, CreateExpressionOptions options);

        void Compile(CompileContext ctx, List<IDataNode> inputs, ConstValue result);
    }
}
