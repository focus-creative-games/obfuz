namespace Obfuz.Virtualization
{
    public interface IRandom
    {
        int NextInt(int min, int max);

        int NextInt(int max);
    }
}
