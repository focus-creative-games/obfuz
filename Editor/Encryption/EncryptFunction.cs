using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Encryption
{

    public class EncryptFunction : EncryptInstructionBase
    {
        private readonly IEncryptInstruction[] _instructions;

        public EncryptFunction(IEncryptInstruction[] instructions)
        {
            _instructions = instructions;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            foreach (var instruction in _instructions)
            {
                value = instruction.Encrypt(value, secretKey, salt);
            }
            return value;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            for (int i = _instructions.Length - 1; i >= 0; i--)
            {
                value = _instructions[i].Decrypt(value, secretKey, salt);
            }
            return value;
        }
    }
}
