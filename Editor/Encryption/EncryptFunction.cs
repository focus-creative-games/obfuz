using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Encryption
{
    public interface IEncryptInstruction
    {
        int Encrypt(int value, int[] secretKey, int salt);

        int Decrypt(int value, int[] secretKey, int salt);
    }

    public abstract class EncryptInstructionBase : IEncryptInstruction
    {
        public abstract int Encrypt(int value, int[] secretKey, int salt);
        public abstract int Decrypt(int value, int[] secretKey, int salt);
    }

    public class AddInstruction : EncryptInstructionBase
    {
        private readonly int _addValue;
        private readonly int _opKeyIndex;

        public AddInstruction(int addValue, int opKeyIndex)
        {
            _addValue = addValue;
            _opKeyIndex = opKeyIndex;
        }
        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return value + secretKey[_opKeyIndex] + salt + _addValue;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return value - secretKey[_opKeyIndex] - salt - _addValue;
        }
    }

    public class  XorInstruction : EncryptInstructionBase
    {
        private readonly int _xorValue;
        private readonly int _opKeyIndex;

        public XorInstruction(int xorValue, int opKeyIndex)
        {
            _xorValue = xorValue;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return value ^ secretKey[_opKeyIndex] ^ salt ^ _xorValue;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return value ^ secretKey[_opKeyIndex] ^ salt ^ _xorValue;
        }
    }

    public class BitRotateInstruction : EncryptInstructionBase
    {
        private readonly int _rotateBitNum;
        private readonly int _opKeyIndex;

        public BitRotateInstruction(int rotateBitNum, int opKeyIndex)
        {
            _rotateBitNum = rotateBitNum;
            _opKeyIndex = opKeyIndex;
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            uint part1 = (uint)value << _rotateBitNum;
            uint part2 = (uint)value >> (32 - _rotateBitNum);
            return ((int)(part1 | part2) ^ secretKey[_opKeyIndex]) + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            uint value2 = (uint)((value - salt) ^ secretKey[_opKeyIndex]);
            uint part1 = value2 >> _rotateBitNum;
            uint part2 = value2 << (32 - _rotateBitNum);
            return (int)(part1 | part2);
        }
    }

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
