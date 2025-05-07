using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class EncryptionCompileContext
    {
        public ModuleDef module;

        public DefaultModuleMetadataImporter DefaultModuleMetadataImporter => MetadataImporter.Instance.GetDefaultModuleMetadataImporter(module);
    }

    public interface IVariableTransformer
    {
        object Compute(object value);

        object ReverseCompute(object value);

        void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx);

        void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx);
    }

    public abstract class VariableTransformerBase : IVariableTransformer
    {
        public abstract object Compute(object value);
        public abstract object ReverseCompute(object value);
        public abstract void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx);
        public abstract void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx);
    }

    public class AddVariableTransformer : VariableTransformerBase
    {
        private readonly DataNodeType _type;
        private readonly object _addValue;

        public AddVariableTransformer(DataNodeType type, object addValue)
        {
            _type = type;
            _addValue = addValue;
        }

        public override object Compute(object value)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                return (int)value + (int)_addValue;
                case DataNodeType.Int64:
                return (long)value + (long)_addValue;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override object ReverseCompute(object value)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                return (int)value - (int)_addValue;
                case DataNodeType.Int64:
                return (long)value - (long)_addValue;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Ldc_I4, (int)_addValue));
                output.Add(Instruction.Create(OpCodes.Add));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Ldc_I8, (long)_addValue));
                output.Add(Instruction.Create(OpCodes.Add));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Ldc_I4, -(int)_addValue));
                output.Add(Instruction.Create(OpCodes.Add));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Ldc_I8, -(long)_addValue));
                output.Add(Instruction.Create(OpCodes.Add));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public static AddVariableTransformer Create(DataNodeType type, IRandom random)
        {
            switch (type)
            {
                case DataNodeType.Int32:  return new AddVariableTransformer(type, random.NextInt());
                case DataNodeType.Int64: return new AddVariableTransformer(type, random.NextLong());
                default:
                throw new NotSupportedException($"Unsupported type: {type}");
            }
        }
    }

    public class XorVariableTransformer : VariableTransformerBase
    {
        private readonly DataNodeType _type;
        private readonly object _xorValue;
        public XorVariableTransformer(DataNodeType type, object xorValue)
        {
            _type = type;
            _xorValue = xorValue;
        }

        public override object Compute(object value)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                return (int)value ^ (int)_xorValue;
                case DataNodeType.Int64:
                return (long)value ^ (long)_xorValue;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override object ReverseCompute(object value)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                return (int)value ^ (int)_xorValue;
                case DataNodeType.Int64:
                return (long)value ^ (long)_xorValue;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Ldc_I4, (int)_xorValue));
                output.Add(Instruction.Create(OpCodes.Xor));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Ldc_I8, (long)_xorValue));
                output.Add(Instruction.Create(OpCodes.Xor));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public override void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_type)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Ldc_I4, (int)_xorValue));
                output.Add(Instruction.Create(OpCodes.Xor));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Ldc_I8, (long)_xorValue));
                output.Add(Instruction.Create(OpCodes.Xor));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_type}");
            }
        }

        public static XorVariableTransformer Create(DataNodeType type, IRandom random)
        {
            switch (type)
            {
                case DataNodeType.Int32: return new XorVariableTransformer(type, random.NextInt());
                case DataNodeType.Int64: return new XorVariableTransformer(type, random.NextLong());
                default:
                throw new NotSupportedException($"Unsupported type: {type}");
            }
        }
    }

    public class CastFloatAsIntTransformer : VariableTransformerBase
    {
        private readonly DataNodeType _outputType;

        public CastFloatAsIntTransformer(DataNodeType srcType)
        {
            switch (srcType)
            {
                case DataNodeType.Float32: _outputType = DataNodeType.Int32; break;
                case DataNodeType.Float64: _outputType = DataNodeType.Int64; break;
                default: throw new NotSupportedException($"Unsupported type: {srcType}");
            }
        }

        public override object Compute(object value)
        {
            switch (_outputType)
            {
                case DataNodeType.Int32:
                    return ConstUtility.CastFloatAsInt((float)value);
                case DataNodeType.Int64:
                return ConstUtility.CastDoubleAsLong((double)value);
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override object ReverseCompute(object value)
        {
            switch (_outputType)
            {
                case DataNodeType.Int32:
                return ConstUtility.CastIntAsFloat((int)value);
                case DataNodeType.Int64:
                return ConstUtility.CastLongAsDouble((long)value);
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_outputType)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastFloatAsInt));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastDoubleAsLong));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_outputType)
            {
                case DataNodeType.Int32:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastIntAsFloat));
                break;
                case DataNodeType.Int64:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastLongAsDouble));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public static CastFloatAsIntTransformer Create(DataNodeType type)
        {
            switch (type)
            {
                case DataNodeType.Float32:
                case DataNodeType.Float64: return new CastFloatAsIntTransformer(type);
                default:
                throw new NotSupportedException($"Unsupported type: {type}");
            }
        }
    }

    public class CastIntAsFloatTransformer : VariableTransformerBase
    {
        private readonly DataNodeType _outputType;

        public CastIntAsFloatTransformer(DataNodeType srcType)
        {
            switch (srcType)
            {
                case DataNodeType.Int32: _outputType = DataNodeType.Float32; break;
                case DataNodeType.Int64: _outputType = DataNodeType.Float64; break;
                default: throw new NotSupportedException($"Unsupported type: {srcType}");
            }
        }

        public override object Compute(object value)
        {
            switch (_outputType)
            {
                case DataNodeType.Float32:
                return ConstUtility.CastIntAsFloat((int)value);
                case DataNodeType.Float64:
                return ConstUtility.CastLongAsDouble((long)value);
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override object ReverseCompute(object value)
        {
            switch (_outputType)
            {
                case DataNodeType.Float32:
                return ConstUtility.CastFloatAsInt((float)value);
                case DataNodeType.Float64:
                return ConstUtility.CastDoubleAsLong((double)value);
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_outputType)
            {
                case DataNodeType.Float32:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastIntAsFloat));
                break;
                case DataNodeType.Float64:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastLongAsDouble));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public override void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            switch (_outputType)
            {
                case DataNodeType.Float32:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastFloatAsInt));
                break;
                case DataNodeType.Float64:
                output.Add(Instruction.Create(OpCodes.Call, ctx.DefaultModuleMetadataImporter.CastDoubleAsLong));
                break;
                default: throw new NotSupportedException($"Unsupported type: {_outputType}");
            }
        }

        public static CastIntAsFloatTransformer Create(DataNodeType type)
        {
            switch (type)
            {
                case DataNodeType.Int32:
                case DataNodeType.Int64: return new CastIntAsFloatTransformer(type);
                default:
                throw new NotSupportedException($"Unsupported type: {type}");
            }
        }
    }

    public class VariableEncryption
    {
        private readonly List<IVariableTransformer> _transformers = new List<IVariableTransformer>();

        private readonly DataNodeType _type;

        public VariableEncryption(DataNodeType type, IRandom random)
        {
            _type = type;
            if (type == DataNodeType.Float32)
            {
                _transformers.Add(CastFloatAsIntTransformer.Create(type));
                _transformers.AddRange(CreateTransformers(DataNodeType.Int32, random));
                _transformers.Add(CastIntAsFloatTransformer.Create(DataNodeType.Int32));

                Assert.AreEqual(1.0f, (float)ComputeValueAfterRevertTransform(ComputeValueAfterTransform(1.0f, _transformers), _transformers));
            }
            else if (type == DataNodeType.Float64)
            {
                _transformers.Add(CastFloatAsIntTransformer.Create(type));
                _transformers.AddRange(CreateTransformers(DataNodeType.Int64, random));
                _transformers.Add(CastIntAsFloatTransformer.Create(DataNodeType.Int64));
                Assert.AreEqual(1.0, (double)ComputeValueAfterRevertTransform(ComputeValueAfterTransform(1.0, _transformers), _transformers));
            }
            else if (type == DataNodeType.Int32)
            {
                _transformers.AddRange(CreateTransformers(type, random));
                Assert.AreEqual(1, (int)ComputeValueAfterRevertTransform(ComputeValueAfterTransform(1, _transformers), _transformers));
            }
            else if (type == DataNodeType.Int64)
            {
                _transformers.AddRange(CreateTransformers(type, random));
                Assert.AreEqual(1L, (long)ComputeValueAfterRevertTransform(ComputeValueAfterTransform(1L, _transformers), _transformers));
            }
            else
            {
                throw new NotSupportedException($"Unsupported type: {type} for VariableEncryption");
            }
        }

        private List<IVariableTransformer> CreateTransformers(DataNodeType type, IRandom random)
        {
            var output = new List<IVariableTransformer>();
            output.Add(XorVariableTransformer.Create(type, random));
            output.Add(AddVariableTransformer.Create(type, random));
            output.Add(XorVariableTransformer.Create(type, random));
            //int count = 3;
            //for (int i = 0; i < count; i++)
            //{
            //    switch(random.NextInt(2))
            //    {
            //        case 0:
            //        {
            //            var transformer = AddVariableTransformer.Create(type, random);
            //            output.Add(transformer);
            //            break;
            //        }
            //        case 1:
            //        {
            //            var transformer = XorVariableTransformer.Create(type, random);
            //            output.Add(transformer);
            //            break;
            //        }
            //    }
            //}
            AddMapZeroToZeroTransform(type, random, output);
            return output;
        }

        public static object ComputeValueAfterTransform(object value, List<IVariableTransformer> transformers)
        {
            foreach (var transformer in transformers)
            {
                value = transformer.Compute(value);
            }
            return value;
        }

        public static object ComputeValueAfterRevertTransform(object value, List<IVariableTransformer> transformers)
        {
            for (int i = transformers.Count - 1; i >= 0; i--)
            {
                var transformer = transformers[i];
                value = transformer.ReverseCompute(value);
            }
            return value;
        }


        private void AddMapZeroToZeroTransform(DataNodeType type, IRandom random, List<IVariableTransformer> output)
        {
            switch (type)
            {
                case DataNodeType.Int32:
                {
                    int value = (int)ComputeValueAfterTransform(0, output);
                    if (value != 0)
                    {
                        output.Add(new AddVariableTransformer(type, -value));
                    }
                    break;
                }
                case DataNodeType.Int64:
                {
                    long value = (long)ComputeValueAfterTransform(0L, output);
                    if (value != 0)
                    {
                        output.Add(new AddVariableTransformer(type, -value));
                    }
                    break;
                }
                default:
                throw new NotSupportedException($"Unsupported type: {type}");
            }
        }

        public void EmitTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            foreach (var transformer in _transformers)
            {
                transformer.EmitTransform(output, ctx);
            }
        }

        public void EmitRevertTransform(List<Instruction> output, EncryptionCompileContext ctx)
        {
            for (int i = _transformers.Count - 1; i >= 0; i--)
            {
                var transformer = _transformers[i];
                transformer.EmitRevertTransform(output, ctx);
            }
        }
    }
}
