using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit
{
    public struct DynamicProxyMethodData
    {
        public MethodDef proxyMethod;
        public int methodId;
    }

    class ModuleDynamicProxyMethodAllocator
    {
        private readonly ModuleDef _module;
        private readonly IRandom _random;

        public ModuleDynamicProxyMethodAllocator(ModuleDef module, IRandom random)
        {
            _module = module;
            _random = random;
        }

        public DynamicProxyMethodData Allocate(IMethod method)
        {
            return default;
        }

        public void Done()
        {

        }
    }

    public class DynamicProxyMethodAllocator
    {
        private readonly IRandom _random;

        private readonly Dictionary<ModuleDef, ModuleDynamicProxyMethodAllocator> _moduleAllocators = new Dictionary<ModuleDef, ModuleDynamicProxyMethodAllocator>();

        public DynamicProxyMethodAllocator(IRandom random)
        {
            _random = random;
        }

        public DynamicProxyMethodData Allocate(ModuleDef mod, IMethod method)
        {
            if (!_moduleAllocators.TryGetValue(mod, out var allocator))
            {
                allocator = new ModuleDynamicProxyMethodAllocator(mod, _random);
                _moduleAllocators.Add(mod, allocator);
            }
            return allocator.Allocate(method);
        }

        public void Done()
        {
            foreach (var allocator in _moduleAllocators.Values)
            {
                allocator.Done();
            }
            _moduleAllocators.Clear();
        }
    }
}
