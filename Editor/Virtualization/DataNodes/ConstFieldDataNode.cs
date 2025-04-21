using System;

namespace Obfuz.Virtualization
{
    public class ConstFieldDataNode : DataNodeAny
    {

        public override void Compile(CompileContext ctx)
        {
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
                case DataNodeType.Float32:
                {
                    // create ldloc.r4
                    break;
                }
                case DataNodeType.Float64:
                {
                    // create ldloc.r8
                    break;
                }
                case DataNodeType.String:
                {
                    // create ldstr
                    break;
                }
                case DataNodeType.Bytes:
                {
                    // create ldstr

                    // RuntimeHelpers.InitializeArray(array, fieldHandle);
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
