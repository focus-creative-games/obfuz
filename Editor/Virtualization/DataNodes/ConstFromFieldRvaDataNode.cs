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

namespace Obfuz.Virtualization
{
    [NodeOutput(DataNodeType.Int32)]
    [NodeOutput(DataNodeType.Int64)]
    [NodeOutput(DataNodeType.Float32)]
    [NodeOutput(DataNodeType.Float64)]
    [NodeOutput(DataNodeType.Bytes)]
    [NodeOutput(DataNodeType.String)]
    public class ConstFromFieldRvaDataNode : DataNodeAny
    {

        private RvaData AllocateRvaData(CompileContext ctx)
        {
            ModuleDef mod = ctx.method.Module;
            RvaDataAllocator allocator = ctx.rvaDataAllocator;
            switch (Type)
            {
                case DataNodeType.Int32: return allocator.Allocate(mod, IntValue);
                case DataNodeType.Int64: return allocator.Allocate(mod, LongValue);
                case DataNodeType.Float32: return allocator.Allocate(mod, FloatValue);
                case DataNodeType.Float64: return allocator.Allocate(mod, DoubleValue);
                case DataNodeType.Bytes: return allocator.Allocate(mod, BytesValue);
                case DataNodeType.String: return allocator.Allocate(mod, StringValue);
                default:
                throw new NotSupportedException($"Unsupported type: {Type}.");
            }
        }

        private static IMethod s_convertInt;
        private static IMethod s_convertLong;
        private static IMethod s_convertFloat;
        private static IMethod s_convertDouble;

        private static IMethod s_convertString;
        private static IField s_Encoding_Utf8;
        private static IMethod s_convertBytes;

        private void InitImportMetadatas(ModuleDef mod)
        {
            if (s_convertInt != null)
            {
                return;
            }
            s_convertInt = mod.Import(typeof(ConstUtility).GetMethod("GetInt", new[] { typeof(byte[]), typeof(int) }));
            s_convertLong = mod.Import(typeof(ConstUtility).GetMethod("GetLong", new[] { typeof(byte[]), typeof(int) }));
            s_convertFloat = mod.Import(typeof(ConstUtility).GetMethod("GetFloat", new[] { typeof(byte[]), typeof(int) }));
            s_convertDouble = mod.Import(typeof(ConstUtility).GetMethod("GetDouble", new[] { typeof(byte[]), typeof(int) }));
            s_convertString = mod.Import(typeof(ConstUtility).GetMethod("GetString", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            //s_Encoding_Utf8 = mod.Import(typeof(Encoding).GetField("UTF8"));
            s_convertBytes = mod.Import(typeof(ConstUtility).GetMethod("GetBytes", new[] { typeof(byte[]), typeof(int), typeof(int) }));
        }

        IMethod GetConvertMethod(ModuleDef mod)
        {
            InitImportMetadatas(mod);
            switch (Type)
            {
                case DataNodeType.Int32: return s_convertInt;
                case DataNodeType.Int64: return s_convertLong;
                case DataNodeType.Float32: return s_convertFloat;
                case DataNodeType.Float64: return s_convertDouble;
                case DataNodeType.String: return s_convertString;
                case DataNodeType.Bytes: return s_convertBytes;
                default: throw new NotSupportedException($"Unsupported type: {Type}.");
            }
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
            switch (Type)
            {
                case DataNodeType.Int32:
                case DataNodeType.Int64:
                case DataNodeType.Float32:
                case DataNodeType.Float64:
                {
                    ctx.output.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                    output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.offset));
                    output.Add(Instruction.Create(OpCodes.Call, convertMethod));
                    break;
                }
                case DataNodeType.String:
                case DataNodeType.Bytes:
                {

                    // Encoding.UTF8.GetString(data, offset, length);
                    //ctx.output.Add(Instruction.Create(OpCodes.Ldsfld, s_Encoding_Utf8));
                    //ctx.output.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                    //output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.offset));
                    //output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.size));
                    //output.Add(Instruction.Create(OpCodes.Call, convertMethod));

                    ctx.output.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                    output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.offset));
                    output.Add(Instruction.Create(OpCodes.Ldc_I4, rvaData.size));
                    output.Add(Instruction.Create(OpCodes.Call, convertMethod));
                    break;
                }
                default:
                throw new NotSupportedException($"Unsupported type: {Type}.");
            }
        }
    }
}
