namespace Obfuz.Virtualization
{
    public interface IFunction
    {
        DataNodeType ReturnType { get; }

        ConstExpression CreateCallable(DataNodeType type, object value, CreateExpressionOptions options);
    }
}
