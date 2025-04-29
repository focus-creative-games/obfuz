using dnlib.DotNet;

namespace Obfuz.DynamicProxy
{
    public abstract class ProxyCallPolicyBase : IProxyCallPolicy
    {
        public abstract bool NeedDynamicProxyCallInMethod(MethodDef method);

        public abstract bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir);
    }
}
