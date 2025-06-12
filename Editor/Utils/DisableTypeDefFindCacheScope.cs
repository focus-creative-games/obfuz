using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public class DisableTypeDefFindCacheScope : IDisposable
    {
        private readonly ModuleDef _module;

        public DisableTypeDefFindCacheScope(ModuleDef module)
        {
            _module = module;
            _module.EnableTypeDefFindCache = false;
        }

        public void Dispose()
        {
            _module.EnableTypeDefFindCache = true;
        }
    }
}
