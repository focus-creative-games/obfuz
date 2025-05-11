namespace Obfuz.Encryption
{
    public class VirtualMachine
    {
        public const int SecretKeyLength = 1024;

        public readonly int vmSeed;
        public readonly EncryptOpCode[] opCodes;

        public VirtualMachine(int vmSeed, EncryptOpCode[] opCodes)
        {
            this.vmSeed = vmSeed;
            this.opCodes = opCodes;
        }
    }
}
