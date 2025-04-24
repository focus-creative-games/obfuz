using dnlib.DotNet;

namespace Obfuz.DynamicProxy
{
    public abstract class DynamicProxyPolicyBase : IDynamicProxyPolicy
    {
        public abstract bool NeedDynamicProxyCallInMethod(MethodDef method);

        public abstract bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir);
    }
}
