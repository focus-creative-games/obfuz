using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Obfuz.Encryption
{
    public class VirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly int vmSeed;
        public readonly EncryptOpCode[] opCodes;

        public VirtualMachine(int vmSeed, EncryptOpCode[] opCodes)
        {
            this.vmSeed = vmSeed;
            this.opCodes = opCodes;
        }
    }

    public interface IVirtualMachineCreator
    {
        VirtualMachine CreateVirtualMachine(int opCodeCount, int vmSeed);
    }

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

    public class EncryptVirtualMachineSimulator
    {
        private readonly EncryptOpCode[] _opCodes;
        private readonly int[] _secretKey;

        public EncryptVirtualMachineSimulator(EncryptOpCode[] opCodes, int[] secretKey)
        {
            _opCodes = opCodes;
            // should be power of 2
            Assert.AreEqual(0, opCodes.Length ^ (opCodes.Length - 1));
            _secretKey = secretKey;
        }

        private List<ushort> DecodeOps(int ops)
        {
            var codes = new List<ushort>();
            while (ops > 0)
            {
                var code = (ushort)(ops % _opCodes.Length);
                codes.Add(code);
                ops >>= 16;
            }
            return codes;
        }

        public int Encrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps(ops);
            foreach (var code in codes)
            {
                var opCode = _opCodes[code];
                value = opCode.Encrypt(value, _secretKey, salt);
            }
            return value;
        }

        public int Decrypt(int value, int ops, int salt)
        {
            var codes = DecodeOps(ops);
            for (int i = codes.Count - 1; i >= 0; i--)
            {
                var opCode = _opCodes[codes[i]];
                value = opCode.Decrypt(value, _secretKey, salt);
            }
            return value;
        }

        public long Encrypt(long value, int ops, int salt)
        {
            int low = (int)(value & 0xFFFFFFFF);
            int high = (int)((value >> 32) & 0xFFFFFFFF);
            var codes = DecodeOps(ops);
            
            // TODO we should encrypt high with encLow
            int encLow = Encrypt(low, ops, salt);
            int encHigh = Encrypt(high, ops, salt);
            
            return ((long)encHigh << 32) | (long)(uint)(encLow);
        }

        public long Decrypt(long value, int ops, int salt)
        {
            int low = (int)(value & 0xFFFFFFFF);
            int high = (int)((value >> 32) & 0xFFFFFFFF);
            var codes = DecodeOps(ops);

            // TODO we should encrypt high with encLow
            int decLow = Decrypt(low, ops, salt);
            int decHigh = Decrypt(high, ops, salt);

            return ((long)decHigh << 32) | (long)(uint)(decLow);
        }

        public float Encrypt(float value, int ops, int salt)
        {
            int intValue = UnsafeUtility.As<float, int>(ref value);
            int encValue = Encrypt(intValue, ops, salt);
            return UnsafeUtility.As<int, float>(ref encValue);
        }

        public float Decrypt(float value, int ops, int salt)
        {
            int intValue = UnsafeUtility.As<float, int>(ref value);
            int decValue = Decrypt(intValue, ops, salt);
            return UnsafeUtility.As<int, float>(ref decValue);
        }

        public double Encrypt(double value, int ops, int salt)
        {
            long longValue = UnsafeUtility.As<double, long>(ref value);
            long encValue = Encrypt(longValue, ops, salt);
            return UnsafeUtility.As<long, double>(ref encValue);
        }

        public double Decrypt(double value, int ops, int salt)
        {
            long longValue = UnsafeUtility.As<double, long>(ref value);
            long decValue = Decrypt(longValue, ops, salt);
            return UnsafeUtility.As<long, double>(ref decValue);
        }

        public unsafe byte[] Encrypt(byte[] bytes, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            // align size to 4
            int align4Length = (length + 3) & ~3;
            byte[] encInts = new byte[align4Length];
            Buffer.BlockCopy(bytes, offset, encInts, 0, length);

            int intLength = align4Length >> 2;
            fixed (byte* intArr = &bytes[0])
            {
                for (int i = 0; i < intLength; i++)
                {
                    int* ele = (int*)intArr + i;
                    *ele = Encrypt(*ele, ops, salt);
                }
            }
            return encInts;
        }

        public unsafe byte[] Decrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<byte>();
            }
            int align4Length = (length + 3) & ~3;
            int intLength = align4Length >> 2;
            byte[] decInts = new byte[align4Length];
            fixed (byte* intArr = &decInts[0])
            {
                for (int i = 0; i < intLength; i++)
                {
                    int* ele = (int*)intArr + i;
                    *ele = Decrypt(*ele, ops, salt);
                }
            }
            return decInts;
        }

        public byte[] Encrypt(byte[] bytes, int ops, int salt)
        {
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public byte[] Encrypt(string value, int ops, int salt)
        {
            if (value.Length == 0)
            {
                return Array.Empty<byte>();
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public string DecryptString(byte[] value, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return string.Empty;
            }
            return System.Text.Encoding.UTF8.GetString(Decrypt(value, offset, length, ops, salt));
        }
    }
}
