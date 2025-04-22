using dnlib.DotNet;

namespace Obfuz.Virtualization
{
    public abstract class DataNodeBase : IDataNode
    {
        public DataNodeType Type { get; set; }

        public abstract object Value { get; set; }

        public int IntValue => (int)Value;

        public long LongValue => (long) Value;

        public float FloatValue => (float)Value;

        public double DoubleValue => (double)Value;

        public string StringValue => (string)Value;

        public byte[] BytesValue => (byte[])Value;

        public virtual void Init(CreateExpressionOptions options)
        {

        }

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
