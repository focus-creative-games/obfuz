using dnlib.DotNet.Emit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Networking.UnityWebRequest;

namespace Obfuz.Virtualization.Functions
{
    public class IntRotateShift : FunctionBase
    {

        public override void CreateArguments(DataNodeType type, object v, CreateExpressionOptions options, List<ConstValue> args)
        {
            switch (type)
            {
                case DataNodeType.Int32:
                {
                    int value = (int)v;

                    //  (value >> amount) | (value << (32 - amount))
                    // << amount
                    // value =  b31 b30 .. b0;
                    int leftShiftAmount = options.random.NextInt(8, 16);
                    uint op1 = (uint)value >> leftShiftAmount;
                    int rightShiftAmount = 32 - leftShiftAmount;
                    uint op2 = (uint)value << rightShiftAmount;
                    Assert.AreEqual((uint)value, (op1 << leftShiftAmount) | (op2 >> rightShiftAmount));
                    args.Add(new ConstValue(DataNodeType.Int32, (int)op1));
                    args.Add(new ConstValue(DataNodeType.Int32, leftShiftAmount));
                    args.Add(new ConstValue(DataNodeType.Int32, (int)op2));
                    args.Add(new ConstValue(DataNodeType.Int32, rightShiftAmount));
                    break;
                }
                case DataNodeType.Int64:
                {
                    long value = (long)v;
                    int leftShiftAmount = options.random.NextInt(16, 32);
                    ulong op1 = (ulong)value >> leftShiftAmount;
                    int rightShiftAmount = 64 - leftShiftAmount;
                    ulong op2 = (ulong)value << rightShiftAmount;
                    Assert.AreEqual((ulong)value, (op1 << leftShiftAmount) | (op2 >> rightShiftAmount));
                    args.Add(new ConstValue(DataNodeType.Int64, (long)op1));
                    args.Add(new ConstValue(DataNodeType.Int32, leftShiftAmount));
                    args.Add(new ConstValue(DataNodeType.Int64, (long)op2));
                    args.Add(new ConstValue(DataNodeType.Int32, rightShiftAmount));
                    break;
                }
                default: throw new NotSupportedException($"Unsupported type {type} for IntRotateShift");
            }
        }

        public override void CompileSelf(CompileContext ctx, List<Instruction> output)
        {
            output.Add(Instruction.Create(OpCodes.Xor));
        }

        public override void Compile(CompileContext ctx, List<IDataNode> inputs, ConstValue result)
        {
            Assert.AreEqual(inputs.Count, 4);
            inputs[0].Compile(ctx);
            inputs[1].Compile(ctx);
            ctx.output.Add(Instruction.Create(OpCodes.Shl));
            inputs[2].Compile(ctx);
            inputs[3].Compile(ctx);
            ctx.output.Add(Instruction.Create(OpCodes.Shr_Un));
            ctx.output.Add(Instruction.Create(OpCodes.Or));
        }
    }
}
