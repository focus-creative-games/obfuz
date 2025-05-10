using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CallObfus
{

    public struct ObfuscationCachePolicy
    {
        public bool cacheInLoop;
        public bool cacheNotInLoop;
    }

    public interface IObfuscationPolicy
    {
        bool NeedDynamicProxyCallInMethod(MethodDef method);

        ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method);

        bool NeedDynamicProxyCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop);
    }
}
