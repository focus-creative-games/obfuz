using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Obfuz.Encryption
{
    public class VirtualMachine
    {
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

        public RandomVirtualMachineCreator()
        {

        }

        private IEncryptInstruction CreateRandomInstruction(IRandom random, int opCodeCount)
        {
            switch (random.NextInt(3))
            {
                case 0:
                    return new AddInstruction(random.NextInt(), random.NextInt(opCodeCount));
                case 1:
                    return new XorInstruction(random.NextInt(), random.NextInt(opCodeCount));
                case 2:
                    return new BitRotateInstruction(random.NextInt(32), random.NextInt(opCodeCount));
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
                insts[i] = CreateRandomInstruction(r, opCodeCount);
            }
            var function = new EncryptFunction(insts);
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

        public int[] Encrypt(byte[] bytes, int offset, int length, int ops, int salt)
        {
            if (length == 0)
            {
                return Array.Empty<int>();
            }
            int intLength = (length + 3) / 4;
            int[] encInts = new int[intLength];
            Buffer.BlockCopy(bytes, offset, encInts, 0, length);
            for (int i = 0; i < intLength; i++)
            {
                encInts[i] = Encrypt(encInts[i], ops, salt);
            }
            return encInts;
        }

        public byte[] Decrypt(int[] value, int offset, int byteLength, int ops, int salt)
        {
            if (byteLength == 0)
            {
                return Array.Empty<byte>();
            }
            int intLength = (byteLength + 3) / 4;
            int[] decValue = new int[intLength];
            for (int i = 0; i < intLength; i++)
            {
                decValue[i] = Decrypt(value[i], ops, salt);
            }

            byte[] bytes = new byte[byteLength];
            Buffer.BlockCopy(decValue, 0, bytes, 0, byteLength);
            return bytes;
        }

        public int[] Encrypt(string value, int ops, int salt)
        {
            if (value.Length == 0)
            {
                return Array.Empty<int>();
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public string DecryptString(int[] value, int offset, int stringBytesLength, int ops, int salt)
        {
            if (stringBytesLength == 0)
            {
                return string.Empty;
            }
            int intLength = (stringBytesLength + 3) / 4;
            int[] intValue = new int[intLength];
            for (int i = 0; i < intLength; i++)
            {
                intValue[i] = Decrypt(value[i], ops, salt);
            }

            byte[] bytes = new byte[stringBytesLength];
            Buffer.BlockCopy(intValue, 0, bytes, 0, stringBytesLength);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
