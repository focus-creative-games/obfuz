namespace Obfuz.Encryption
{
    public abstract class EncryptInstructionBase : IEncryptInstruction
    {
        public abstract int Encrypt(int value, int[] secretKey, int salt);
        public abstract int Decrypt(int value, int[] secretKey, int salt);
    }
}
