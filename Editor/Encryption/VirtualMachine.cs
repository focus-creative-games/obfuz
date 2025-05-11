namespace Obfuz.Encryption
{
    public class VirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly byte[] secretKey;
        public readonly EncryptOpCode[] opCodes;

        public VirtualMachine(byte[] secretKey, EncryptOpCode[] opCodes)
        {
            this.secretKey = secretKey;
            this.opCodes = opCodes;
        }
    }
}
