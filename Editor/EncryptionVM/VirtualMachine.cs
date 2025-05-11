namespace Obfuz.EncryptionVM
{
    public class VirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly int[] secretKey;
        public readonly EncryptionInstructionWithOpCode[] opCodes;

        public VirtualMachine(int[] secretKey, EncryptionInstructionWithOpCode[] opCodes)
        {
            this.secretKey = secretKey;
            this.opCodes = opCodes;
        }
    }
}
