using Obfuz.Utils;
using UnityEngine.Assertions;

namespace Obfuz.Encryption
{
    public class RandomVirtualMachineCreator : IVirtualMachineCreator
    {
        private readonly byte[] _byteSecretKey;
        private readonly int[] _secretKey;
        private readonly IRandom _random;

        public RandomVirtualMachineCreator(byte[] secretKey)
        {
            _byteSecretKey = secretKey;
            _secretKey = KeyGenerator.ConvertToIntKey(secretKey);
            _random = new RandomWithKey(secretKey, 0);
        }

        private IEncryptInstruction CreateRandomInstruction(int secretKeyLength)
        {
            switch (_random.NextInt(3))
            {
                case 0:
                    return new AddInstruction(_random.NextInt(), _random.NextInt(secretKeyLength));
                case 1:
                    return new XorInstruction(_random.NextInt(), _random.NextInt(secretKeyLength));
                case 2:
                    return new BitRotateInstruction(_random.NextInt(32), _random.NextInt(secretKeyLength));
                default:
                throw new System.Exception("Invalid instruction type");
            }
        }

        private EncryptOpCode CreateEncryptOpCode(ushort code, int opCodeCount)
        {
            Assert.IsTrue(code < opCodeCount);
            var insts = new IEncryptInstruction[opCodeCount];
            for (int i = 0; i < insts.Length; i++)
            {
                IEncryptInstruction inst = CreateRandomInstruction(VirtualMachine.SecretKeyLength);
                Assert.AreEqual(1234, inst.Decrypt(inst.Encrypt(1234, _secretKey, i), _secretKey, i));
                insts[i] = CreateRandomInstruction(opCodeCount);
            }
            var function = new EncryptFunction(insts);
            Assert.AreEqual(1234, function.Decrypt(function.Encrypt(1234, _secretKey, code), _secretKey, code));
            return new EncryptOpCode(code, function);
        }

        public VirtualMachine CreateVirtualMachine(int opCodeCount)
        {
            Assert.IsTrue(opCodeCount > 0);
            Assert.AreEqual(0, opCodeCount ^ (opCodeCount - 1));
            var opCodes = new EncryptOpCode[opCodeCount];
            for (int i = 0; i < opCodes.Length; i++)
            {
                opCodes[i] = CreateEncryptOpCode((ushort)i, opCodeCount);
            }
            return new VirtualMachine(_byteSecretKey, opCodes);
        }
    }
}
