namespace Obfuz.Encryption
{
    public interface IVirtualMachineCreator
    {
        VirtualMachine CreateVirtualMachine(int opCodeCount);
    }
}
