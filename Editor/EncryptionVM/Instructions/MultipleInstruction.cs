using Obfuz.Utils;
using System.Collections.Generic;

namespace Obfuz.EncryptionVM.Instructions
{
    public class MultipleInstruction : EncryptionInstructionBase
    {
        private readonly int _multiValue;
        private readonly int _revertMultiValue;
        private readonly int _opKeyIndex;

        public MultipleInstruction(int addValue, int opKeyIndex)
        {
            _multiValue = addValue;
            _opKeyIndex = opKeyIndex;
            _revertMultiValue = MathUtil.ModInverse32(addValue);
            Verify();
        }

        private void Verify()
        {
            int a = 1122334;
            UnityEngine.Assertions.Assert.AreEqual(a, a * _multiValue * _revertMultiValue);
        }

        public override int Encrypt(int value, int[] secretKey, int salt)
        {
            return value * _multiValue + secretKey[_opKeyIndex] + salt;
        }

        public override int Decrypt(int value, int[] secretKey, int salt)
        {
            return (value - secretKey[_opKeyIndex] - salt) * _revertMultiValue;
        }

        public override void GenerateEncryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = value *  {_multiValue} + _secretKey[{_opKeyIndex}] + salt;");
        }

        public override void GenerateDecryptCode(List<string> lines, string indent)
        {
            lines.Add(indent + $"value = (value - _secretKey[{_opKeyIndex}] - salt) * {_revertMultiValue};");
        }
    }
}
