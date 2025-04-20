namespace Obfuz.Virtualization
{
    public abstract class DataNodeBase : IDataNode
    {
        public DataNodeType Type { get; protected set; }

        public IDataNode Expr { get; protected set; }

        public abstract object Value { get; protected set; }

        public abstract void Compile(CompileContext ctx);
    }


    public abstract class DataNodeBase<T> : DataNodeBase
    {
        public T Value2 { get; protected set; }

        public override object Value
        {
            get => Value2;
            protected set => Value2 = (T)value;
        }
    }


    public abstract class DataNodeAny : DataNodeBase
    {
        private object _value;
        public override object Value
        {
            get => _value;
            protected set => _value = value;
        }
    }
}
