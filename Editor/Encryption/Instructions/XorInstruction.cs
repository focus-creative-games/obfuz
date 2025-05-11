namespace Obfuz.Encryption.Instructions
{
    public class  XorInstruction : EncryptionInstructionBase
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
}
