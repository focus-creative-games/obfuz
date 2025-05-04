namespace Obfuz.Rename
{
    public interface INameScope
    {
        void AddPreservedName(string name);

        string GetNewName(string originalName, bool reuse);
    }
}
