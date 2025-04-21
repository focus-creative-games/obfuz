namespace Obfuz.Virtualization
{
    public struct ConstValue
    {
        public readonly DataNodeType type;
        public readonly object value;

        public ConstValue(DataNodeType type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }
}
