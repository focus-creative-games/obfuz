using Obfuz.EncryptionVM.Instructions;
using Obfuz.Utils;
using UnityEngine.Assertions;

namespace Obfuz.EncryptionVM
{
    public class VirtualMachineCreator
    {
        private readonly int[] _vmGenerationSecretKey;
        private readonly IRandom _random;

        public const int CodeGenerationSecretKeyLength = 1024;

        public VirtualMachineCreator(string vmGenerationSecretKey, byte[] encryptionSecretKey)
        {
            _vmGenerationSecretKey = KeyGenerator.ConvertToIntKey(KeyGenerator.GenerateKey(vmGenerationSecretKey, CodeGenerationSecretKeyLength));
            _random = new RandomWithKey(encryptionSecretKey, 0);
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
            Assert.AreEqual(1234, inst.Decrypt(inst.Encrypt(1234, _vmGenerationSecretKey, 0x12345678), _vmGenerationSecretKey, 0x12345678));
            return new EncryptionInstructionWithOpCode(code, inst);
        }

        public VirtualMachine CreateVirtualMachine(int opCodeCount)
        {
            Assert.IsTrue(opCodeCount > 0);
            Assert.AreEqual(0, opCodeCount & (opCodeCount - 1));
            var opCodes = new EncryptionInstructionWithOpCode[opCodeCount];
            for (int i = 0; i < opCodes.Length; i++)
            {
                opCodes[i] = CreateEncryptOpCode((ushort)i);
            }
            return new VirtualMachine(_vmGenerationSecretKey, opCodes);
        }
    }
}
