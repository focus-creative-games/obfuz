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
                //case DataNodeType.Byte:
                //{
                //    // create ldloc.i4 
                //    break;
                //}
                case DataNodeType.Int32:
                {
                    // create ldloc.i4
                    break;
                }
                case DataNodeType.Int64:
                {
                    // create ldloc.i8
                    break;
                }
                //case DataNodeType.Float32:
                //{
                //    // create ldloc.r4
                //    break;
                //}
                //case DataNodeType.Float64:
                //{
                //    // create ldloc.r8
                //    break;
                //}
                //case DataNodeType.Null:
                //{
                //    // create ldnull
                //    break;
                //}
                //case DataNodeType.String:
                //{
                //    // create ldstr
                //    break;
                //}
                //case DataNodeType.Bytes:
                //{
                //    // create ldstr

                //    // RuntimeHelpers.InitializeArray(array, fieldHandle);
                //    break;
                //}
                default:
                {
                    throw new NotImplementedException($"Type:{Type} not implemented");
                }
            }
        }
    }
}
