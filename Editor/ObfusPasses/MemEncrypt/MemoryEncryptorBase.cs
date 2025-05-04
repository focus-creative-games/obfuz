using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;

namespace Obfuz.ObfusPasses.MemEncrypt
{
    public abstract class MemoryEncryptorBase : IMemoryEncryptor
    {
        public abstract void Encrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx);
        public abstract void Decrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx);
    }
}
