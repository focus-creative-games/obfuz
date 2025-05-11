using Obfuz.Utils;
using UnityEngine.Assertions;

namespace Obfuz.Encryption
{
    public class RandomVirtualMachineCreator : IVirtualMachineCreator
    {
        private readonly int[] _debugSecretKey;

        public RandomVirtualMachineCreator()
        {
            _debugSecretKey = new int[VirtualMachine.SecretKeyLength];
            for (int i = 0; i < _debugSecretKey.Length; i++)
            {
                _debugSecretKey[i] = i;
            }
        }

        private IEncryptInstruction CreateRandomInstruction(IRandom random, int secretKeyLength)
        {
            switch (random.NextInt(3))
            {
                case 0:
                    return new AddInstruction(random.NextInt(), random.NextInt(secretKeyLength));
                case 1:
                    return new XorInstruction(random.NextInt(), random.NextInt(secretKeyLength));
                case 2:
                    return new BitRotateInstruction(random.NextInt(32), random.NextInt(secretKeyLength));
                default:
                throw new System.Exception("Invalid instruction type");
            }
        }

        private EncryptOpCode CreateEncryptOpCode(ushort code, IRandom r, int opCodeCount)
        {
            Assert.IsTrue(code < opCodeCount);
            var insts = new IEncryptInstruction[opCodeCount];
            for (int i = 0; i < insts.Length; i++)
            {
                IEncryptInstruction inst = CreateRandomInstruction(r, VirtualMachine.SecretKeyLength);
                Assert.AreEqual(1234, inst.Decrypt(inst.Encrypt(1234, _debugSecretKey, i), _debugSecretKey, i));
                insts[i] = CreateRandomInstruction(r, opCodeCount);
            }
            var function = new EncryptFunction(insts);
            Assert.AreEqual(1234, function.Decrypt(function.Encrypt(1234, _debugSecretKey, code), _debugSecretKey, code));
            return new EncryptOpCode(code, function);
        }

        public VirtualMachine CreateVirtualMachine(int opCodeCount, int vmSeed)
        {
            Assert.IsTrue(opCodeCount > 0);
            Assert.AreEqual(0, opCodeCount ^ (opCodeCount - 1));
            IRandom r = new RandomWithKey(new byte[] {1,2,3,4,5,6}, vmSeed);
            var opCodes = new EncryptOpCode[opCodeCount];
            for (int i = 0; i < opCodes.Length; i++)
            {
                opCodes[i] = CreateEncryptOpCode((ushort)i, r, opCodeCount);
            }
            return new VirtualMachine(vmSeed, opCodes);
        }
    }
}
