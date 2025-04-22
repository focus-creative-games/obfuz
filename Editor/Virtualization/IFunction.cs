using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public interface IFunction
    {

        IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options);

        void Compile(CompileContext ctx, List<IDataNode> inputs, ConstValue result);
    }
}
