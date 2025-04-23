using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Obfuz.Virtualization.DataNodes
{
    public class MemoryCastIntAsFloat : FunctionBase
    {
        private static IMethod s_castIntAsFloat;
        private static IMethod s_castLongAsDouble;

        private void InitMetadatas(ModuleDef mod)
        {
            if (s_castIntAsFloat !=  null)
            {
                return;
            }
            var constUtilityType = typeof(ConstUtility);
            s_castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
            Assert.IsNotNull(s_castIntAsFloat, "CastIntAsFloat not found");
            s_castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
            Assert.IsNotNull(s_castLongAsDouble, "CastLongAsDouble not found");
        }

        public override void CompileSelf(CompileContext ctx, List<IDataNode> inputs, List<Instruction> output)
        {
            Assert.AreEqual(1, inputs.Count);
            InitMetadatas(ctx.method.Module);
            switch (inputs[0].Type)
            {
                case DataNodeType.Int32:
                {
                    output.Add(Instruction.Create(OpCodes.Call, s_castIntAsFloat));
                    break;
                }
                case DataNodeType.Int64:
                {
                    output.Add(Instruction.Create(OpCodes.Call, s_castLongAsDouble));
                    break;
                }
                default: throw new NotSupportedException($"Unsupported type {inputs[0].Type} for MemoryCastIntAsFloat");
            }
        }

        public override void CreateArguments(DataNodeType type, object v, CreateExpressionOptions options, List<ConstValue> args)
        {
            switch (type)
            {
                case DataNodeType.Float32:
                {
                    float value = (float)v;
                    int intValue = ConstUtility.CastFloatAsInt(value);
                    args.Add(new ConstValue(DataNodeType.Int32, intValue));
                    break;
                }
                case DataNodeType.Float64:
                {
                    double value = (double)v;
                    long longValue = ConstUtility.CastDoubleAsLong(value);
                    args.Add(new ConstValue(DataNodeType.Int64, longValue));
                    break;
                }
                default:
                {
                    throw new NotImplementedException($"Type:{type} not implemented");
                }
            }
        }
    }
}
