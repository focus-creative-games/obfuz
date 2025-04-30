using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.MemEncrypt
{
    public class MemoryEncryptionContext
    {
        public ModuleDef module;

        public Instruction currentInstruction;
    }

    public interface IMemoryEncryptor
    {
        void Encrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx);

        void Decrypt(FieldDef field, List<Instruction> outputInstructions, MemoryEncryptionContext ctx);
    }
}
