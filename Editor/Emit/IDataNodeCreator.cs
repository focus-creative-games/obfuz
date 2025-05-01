namespace Obfuz.Emit
{
    public interface IDataNodeCreator
    {
        IDataNode CreateRandom(DataNodeType type, object value, CreateExpressionOptions options);
    }
}
