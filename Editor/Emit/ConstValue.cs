using System;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public struct ConstValue
    {
        public readonly DataNodeType type;
        public readonly object value;

        public ConstValue(DataNodeType type, object value)
        {
            switch (type)
            {
                case DataNodeType.Int32: Assert.IsTrue(value is int); break;
                case DataNodeType.Int64: Assert.IsTrue(value is long); break;
                case DataNodeType.Float32: Assert.IsTrue(value is float); break;
                case DataNodeType.Float64: Assert.IsTrue(value is double); break;
                case DataNodeType.String: Assert.IsTrue(value is string); break;
                case DataNodeType.Bytes: Assert.IsTrue(value is byte[]); break;
                default: throw new NotSupportedException($"Unsupported type {type} for ConstValue");
            }
            this.type = type;
            this.value = value;
        }
    }
}
