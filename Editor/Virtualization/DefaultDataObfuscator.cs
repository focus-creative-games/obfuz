using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class DefaultDataObfuscator : IDataObfuscator
    {
        private readonly RandomDataNodeCreator _nodeCreator = new RandomDataNodeCreator();

        public void ObfuscateInt(MethodDef method, int value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.Int32, value);
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(value));
        }

        public void ObfuscateLong(MethodDef method, long value, List<Instruction> obfuscatedInstructions)
        {
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, value));
        }

        public void ObfuscateFloat(MethodDef method, float value, List<Instruction> obfuscatedInstructions)
        {
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R4, value));
        }

        public void ObfuscateDouble(MethodDef method, double value, List<Instruction> obfuscatedInstructions)
        {
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R8, value));
        }

        public void ObfuscateBytes(MethodDef method, Array value, List<Instruction> obfuscatedInstructions)
        {
            throw new NotSupportedException();
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I4, value.Length));
        }

        public void ObfuscateString(MethodDef method, string value, List<Instruction> obfuscatedInstructions)
        {
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldstr, value));
        }
    }
}
