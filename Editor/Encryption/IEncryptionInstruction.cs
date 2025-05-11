namespace Obfuz.Encryption
{
    public interface IEncryptionInstruction
    {
        int Encrypt(int value, int[] secretKey, int salt);

        int Decrypt(int value, int[] secretKey, int salt);
    }

    public abstract class EncryptionInstructionBase : IEncryptionInstruction
    {
        public abstract int Encrypt(int value, int[] secretKey, int salt);
        public abstract int Decrypt(int value, int[] secretKey, int salt);
    }
}
