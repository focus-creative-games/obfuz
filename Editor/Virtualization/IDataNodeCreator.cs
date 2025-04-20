namespace Obfuz.Virtualization
{
    public interface IDataNodeCreator
    {
        IDataNode CreateRandom(DataNodeType type, object value, CreateExpressionOptions options);
    }
}
