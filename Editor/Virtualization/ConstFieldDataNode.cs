using System;

namespace Obfuz.Virtualization
{
    public class ConstFieldDataNode : DataNodeAny
    {

        public override void Compile(CompileContext ctx)
        {

            switch (Type)
            {
                case DataNodeType.Byte:
                {
                    // create ldloc.i4 
                    break;
                }
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
                case DataNodeType.Null:
                {
                    // create ldnull
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
