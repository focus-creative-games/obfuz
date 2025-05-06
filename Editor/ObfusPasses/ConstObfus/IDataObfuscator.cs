using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.ConstObfus
{
    public interface IDataObfuscator
    {
        void ObfuscateInt(MethodDef method, int value, List<Instruction> obfuscatedInstructions);

        void ObfuscateLong(MethodDef method, long value, List<Instruction> obfuscatedInstructions);

        void ObfuscateFloat(MethodDef method, float value, List<Instruction> obfuscatedInstructions);

        void ObfuscateDouble(MethodDef method, double value, List<Instruction> obfuscatedInstructions);

        void ObfuscateString(MethodDef method, string value, List<Instruction> obfuscatedInstructions);

        void ObfuscateBytes(MethodDef method, Array value, List<Instruction> obfuscatedInstructions);

        void Done();
    }
}
