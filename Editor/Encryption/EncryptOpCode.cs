namespace Obfuz.Encryption
{
    public class EncryptOpCode
    {
        public readonly ushort code;

        public readonly EncryptFunction function;

        public EncryptOpCode(ushort code, EncryptFunction function)
        {
            this.code = code;
            this.function = function;
        }

        public int Encrypt(int value, int[] secretKey, int salt)
        {
            return function.Encrypt(value, secretKey, salt);
        }

        public int Decrypt(int value, int[] secretKey, int salt)
        {
            return function.Decrypt(value, secretKey, salt);
        }
    }
}
