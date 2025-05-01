namespace Obfuz.Emit
{
    public abstract class DataNodeCreatorBase : IDataNodeCreator
    {
        public abstract IDataNode CreateRandom(DataNodeType type, object value, CreateExpressionOptions options);
    }
}
