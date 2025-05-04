using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Obfuz.Rename
{

    public class NameMaker : NameMakerBase
    {
        private readonly List<string> _wordSet;

        public NameMaker(List<string> wordSet)
        {
            _wordSet = wordSet;
        }

        protected override INameScope CreateNameScope()
        {
            return new NameScope(_wordSet);
        }
    }
}
