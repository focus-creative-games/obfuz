namespace Obfuz.Virtualization
{
    public abstract class DataNodeBase : IDataNode
    {
        public DataNodeType Type { get; set; }

        public abstract object Value { get; set; }

        public abstract void Compile(CompileContext ctx);
    }


    public abstract class DataNodeBase<T> : DataNodeBase
    {
        public T Value2 { get; set; }

        public override object Value
        {
            get => Value2;
            set => Value2 = (T)value;
        }
    }


    public abstract class DataNodeAny : DataNodeBase
    {
        private object _value;

        public override object Value
        {
            get => _value;
            set => _value = value;
        }
    }
}
