using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit.Functions
{
    public class IntAdd : FunctionBase
    {

        public override void CreateArguments(DataNodeType type, object v, CreateExpressionOptions options, List<ConstValue> args)
        {
            switch (type)
            {
                case DataNodeType.Int32:
                {
                    int value = (int)v;

                    int op1 = options.random.NextInt();
                    int op2 = value - op1;
                    args.Add(new ConstValue(DataNodeType.Int32, op1));
                    args.Add(new ConstValue(DataNodeType.Int32, op2));
                    break;
                }
                case DataNodeType.Int64:
                {
                    long value = (long)v;
                    long op1 = options.random.NextLong();
                    long op2 = value - op1;
                    args.Add(new ConstValue(DataNodeType.Int64, op1));
                    args.Add(new ConstValue(DataNodeType.Int64, op2));
                    break;
                }
                default:
                {
                    throw new NotSupportedException($"Unsupported type: {type}");
                }
            }
        }

        public override void CompileSelf(CompileContext ctx, List<IDataNode> inputs, List<Instruction> output)
        {
            output.Add(Instruction.Create(OpCodes.Add));
        }
    }
}
