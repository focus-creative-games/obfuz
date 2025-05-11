using Obfuz.EncryptionVM.Instructions;
using Obfuz.Utils;
using UnityEngine.Assertions;

namespace Obfuz.EncryptionVM
{
    public class VirtualMachineCreator
    {
        private readonly string _vmGenerationSecretKey;
        private readonly IRandom _random;

        public const int CodeGenerationSecretKeyLength = 1024;

        public const int VirtualMachineVersion = 1;

        public VirtualMachineCreator(string vmGenerationSecretKey)
        {
            _vmGenerationSecretKey = vmGenerationSecretKey;
            byte[] byteGenerationSecretKey = KeyGenerator.GenerateKey(vmGenerationSecretKey, CodeGenerationSecretKeyLength);
            _random = new RandomWithKey(byteGenerationSecretKey, 0);
        }

        private IEncryptionInstruction CreateRandomInstruction(int intSecretKeyLength)
        {
            switch (_random.NextInt(3))
            {
                case 0:
                    return new AddInstruction(_random.NextInt(), _random.NextInt(intSecretKeyLength));
                case 1:
                    return new XorInstruction(_random.NextInt(), _random.NextInt(intSecretKeyLength));
                case 2:
                    return new BitRotateInstruction(_random.NextInt(32), _random.NextInt(intSecretKeyLength));
                default:
                throw new System.Exception("Invalid instruction type");
            }
        }

        private EncryptionInstructionWithOpCode CreateEncryptOpCode(ushort code)
        {
            IEncryptionInstruction inst = CreateRandomInstruction(VirtualMachine.SecretKeyLength / sizeof(int));
            return new EncryptionInstructionWithOpCode(code, inst);
        }

        public VirtualMachine CreateVirtualMachine(int opCodeCount)
        {
            if (opCodeCount < 64)
            {
                throw new System.Exception("OpCode count should be >= 64");
            }
            if ((opCodeCount & (opCodeCount - 1)) != 0)
            {
                throw new System.Exception("OpCode count should be power of 2");
            }
            var opCodes = new EncryptionInstructionWithOpCode[opCodeCount];
            for (int i = 0; i < opCodes.Length; i++)
            {
                opCodes[i] = CreateEncryptOpCode((ushort)i);
            }
            return new VirtualMachine(VirtualMachineVersion, _vmGenerationSecretKey, opCodes);
        }
    }
}
