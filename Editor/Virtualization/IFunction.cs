namespace Obfuz.Virtualization
{
    public interface IFunction
    {
        DataNodeType ReturnType { get; }

        ConstExpression CreateCallable(IDataNode result, CreateExpressionOptions options);
    }
}
