using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus.NameMakers
{
    public class DebugNameMaker : NameMakerBase
    {
        private class DebugNameScope : INameScope
        {

            public bool AddPreservedName(string name)
            {
                return true;
            }

            public string GetNewName(string originalName, bool reuse)
            {
                return $"${originalName}";
            }

            public bool IsNamePreserved(string name)
            {
                return false;
            }
        }

        protected override INameScope CreateNameScope()
        {
            return new DebugNameScope();
        }
    }
}
