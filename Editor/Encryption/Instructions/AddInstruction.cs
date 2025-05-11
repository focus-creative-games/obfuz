namespace Obfuz.Encryption.Instructions
{
    public class AddInstruction : EncryptionInstructionBase
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
}
