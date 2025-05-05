using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus
{

    public class NameScope : NameScopeBase
    {
        private readonly List<string> _wordSet;
        private int _nextIndex;

        public NameScope(List<string> wordSet)
        {
            _wordSet = wordSet;
            _nextIndex = 0;
        }

        protected override void BuildNewName(StringBuilder nameBuilder, string originalName, string lastName)
        {
            for (int i = _nextIndex++; ;)
            {
                nameBuilder.Append(_wordSet[i % _wordSet.Count]);
                i = i / _wordSet.Count;
                if (i == 0)
                {
                    break;
                }
            }
        }
    }
}
