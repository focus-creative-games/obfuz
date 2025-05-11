namespace Obfuz.Encryption
{
    public class EncryptionVirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly int[] secretKey;
        public readonly EncryptionInstructionWithOpCode[] opCodes;

        public EncryptionVirtualMachine(int[] secretKey, EncryptionInstructionWithOpCode[] opCodes)
        {
            this.secretKey = secretKey;
            this.opCodes = opCodes;
        }
    }
}
