using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class DefaultDataObfuscator : IDataObfuscator
    {
        private readonly IRandom _random;
        private readonly RandomDataNodeCreator _nodeCreator;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly IEncryptor _encryptor;

        public DefaultDataObfuscator()
        {
            _random = new RandomWithKey(new byte[] { 0x1, 0x2, 0x3, 0x4 }, 0x5);
            _encryptor = new DefaultEncryptor(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D });
            _nodeCreator = new RandomDataNodeCreator(_random);
            _rvaDataAllocator = new RvaDataAllocator(_random, _encryptor);
            _constFieldAllocator = new ConstFieldAllocator(_random);
        }

        private void CompileNode(IDataNode node, MethodDef method, List<Instruction> obfuscatedInstructions)
        {
            var ctx = new CompileContext
            {
                method = method,
                output = obfuscatedInstructions,
                rvaDataAllocator = _rvaDataAllocator,
                constFieldAllocator = _constFieldAllocator,
            };
            node.Compile(ctx);
        }

        public void ObfuscateInt(MethodDef method, int value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Int32, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I4, value));
        }

        public void ObfuscateLong(MethodDef method, long value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Int64, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, value));
        }

        public void ObfuscateFloat(MethodDef method, float value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Float32, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R4, value));
        }

        public void ObfuscateDouble(MethodDef method, double value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Float64, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R8, value));
        }

        public void ObfuscateBytes(MethodDef method, Array value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Bytes, value);
            CompileNode(node, method, obfuscatedInstructions);
            //throw new NotSupportedException();
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I4, value.Length));
        }

        public void ObfuscateString(MethodDef method, string value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.String, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldstr, value));
        }

        public void Done()
        {
            _rvaDataAllocator.Done();
            _constFieldAllocator.Done();
        }
    }
}
