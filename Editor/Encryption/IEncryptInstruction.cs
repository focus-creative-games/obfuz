namespace Obfuz.Encryption
{
    public interface IEncryptInstruction
    {
        int Encrypt(int value, int[] secretKey, int salt);

        int Decrypt(int value, int[] secretKey, int salt);
    }
}
