using dnlib.DotNet.Emit;
using System;

namespace Obfuz.Virtualization
{
    [NodeOutput(DataNodeType.Int32)]
    [NodeOutput(DataNodeType.Int64)]
    public class ConstDataNode : DataNodeAny
    {

        public override void Compile(CompileContext ctx)
        {

            // only Int32 - Null,
            // to avoid GC,
            // the leaf node that can create string or bytes is ConstFieldDataNode.
            // float and double can only come from RawCastAs int32 and int64.
            // so we only need to deal int32 and int64
            switch (Type)
            {
                case DataNodeType.Int32:
                {
                    // create ldloc.i4
                    ctx.output.Add(Instruction.CreateLdcI4(IntValue));
                    break;
                }
                case DataNodeType.Int64:
                {
                    // create ldloc.i8
                    ctx.output.Add(Instruction.Create(OpCodes.Ldc_I8, LongValue));
                    break;
                }
                default:
                {
                    throw new NotImplementedException($"Type:{Type} not implemented");
                }
            }
        }
    }
}
