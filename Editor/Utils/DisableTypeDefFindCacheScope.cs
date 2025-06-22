using dnlib.DotNet;
using System;

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
