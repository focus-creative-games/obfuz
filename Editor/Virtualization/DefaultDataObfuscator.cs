using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class DefaultDataObfuscator : IDataObfuscator
    {
        public bool ObfuscateInt(MethodDef method, int value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }

        public bool TryObfuscateLong(MethodDef method, long value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }

        public bool TryObfuscateFloat(MethodDef method, float value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }

        public bool TryObfuscateDouble(MethodDef method, double value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }

        public bool TryObfuscateBytes(MethodDef method, Array value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }

        public bool TryObfuscateString(MethodDef method, string value, List<Instruction> obfuscatedInstructions)
        {
            return false;
        }
    }
}
