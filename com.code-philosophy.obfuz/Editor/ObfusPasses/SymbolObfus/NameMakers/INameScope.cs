namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public interface INameScope
    {
        void AddPreservedName(string name);

        string GetNewName(string originalName, bool reuse);
    }
}
