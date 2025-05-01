using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Networking.UnityWebRequest;
using UnityEngine.UIElements;

namespace Obfuz.Emit
{
    [NodeOutput(DataNodeType.Int32)]
    [NodeOutput(DataNodeType.Int64)]
    [NodeOutput(DataNodeType.Float32)]
    [NodeOutput(DataNodeType.Float64)]
    [NodeOutput(DataNodeType.Bytes)]
    [NodeOutput(DataNodeType.String)]
    public class BytesInitializeFromFieldRvaDataNode : DataNodeAny
    {

        private RvaData AllocateRvaData(CompileContext ctx)
        {
            ModuleDef mod = ctx.method.Module;
            RvaDataAllocator allocator = ctx.rvaDataAllocator;
            switch (Type)
            {
                //case DataNodeType.Int32: return allocator.Allocate(mod, IntValue);
                //case DataNodeType.Int64: return allocator.Allocate(mod, LongValue);
                //case DataNodeType.Float32: return allocator.Allocate(mod, FloatValue);
                //case DataNodeType.Float64: return allocator.Allocate(mod, DoubleValue);
                case DataNodeType.Bytes: return allocator.Allocate(mod, BytesValue);
                //case DataNodeType.String: return allocator.Allocate(mod, StringValue);
                default:
                throw new NotSupportedException($"Unsupported type: {Type}.");
            }
        }

        private static IMethod s_convertBytes;

        private void InitImportMetadatas(ModuleDef mod)
        {
            if (s_convertBytes != null)
            {
                return;
            }
            s_convertBytes = mod.Import(typeof(ConstUtility).GetMethod("InitializeArray", new[] { typeof(Array), typeof(byte[]), typeof(int), typeof(int) }));
        }

        IMethod GetConvertMethod(ModuleDef mod)
        {
            InitImportMetadatas(mod);
            return s_convertBytes;
        }

        public override void Compile(CompileContext ctx)
        {
            // only support Int32, int64, bytes.
            // string can only create from StringFromBytesNode
            // x = memcpy array.GetRange(index, length);
            var output = ctx.output;
            RvaData rvaData = AllocateRvaData(ctx);
            ModuleDef mod = ctx.method.Module;
            IMethod convertMethod = GetConvertMethod(mod);

            ctx.output.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.offset));
            output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.size));
            //output.Add(Instruction.Create(OpCodes.Newarr, mod.CorLibTypes.Byte.ToTypeDefOrRef()));
            //output.Add(Instruction.Create(OpCodes.Ldc_I4, 0));
            //output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.size));
            output.Add(Instruction.Create(OpCodes.Call, convertMethod));
        }
    }
}
