namespace Obfuz.Virtualization
{
    public interface IFunction
    {
        ConstExpression CreateCallable(IDataNode result, CreateExpressionOptions options);
    }
}
