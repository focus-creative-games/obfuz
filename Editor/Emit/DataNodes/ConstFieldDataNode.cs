using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Obfuz.Emit
{
    public class ConstFieldDataNode : DataNodeAny
    {

        public override void Compile(CompileContext ctx)
        {
            ModuleDef mod = ctx.method.Module;
            var output = ctx.output;
            FieldDef field;
            switch (Type)
            {
                case DataNodeType.Int32:
                {
                    field = ctx.constFieldAllocator.Allocate(mod, IntValue);
                    break;
                }
                case DataNodeType.Int64:
                {
                    field = ctx.constFieldAllocator.Allocate(mod, LongValue);
                    break;
                }
                case DataNodeType.Float32:
                {
                    field = ctx.constFieldAllocator.Allocate(mod, FloatValue);
                    break;
                }
                case DataNodeType.Float64:
                {
                    field = ctx.constFieldAllocator.Allocate(mod, DoubleValue);
                    break;
                }
                case DataNodeType.String:
                {
                    field = ctx.constFieldAllocator.Allocate(mod, StringValue);
                    break;
                }
                case DataNodeType.Bytes:
                {
                    // ldsfld 
                    // ldtoken 
                    // RuntimeHelpers.InitializeArray(array, fieldHandle);
                    //break;
                    throw new NotSupportedException("Bytes not supported");
                }
                default:
                {
                    throw new NotImplementedException($"Type:{Type} not implemented");
                }
            }
            output.Add(Instruction.Create(OpCodes.Ldsfld, field));
        }
    }
}
