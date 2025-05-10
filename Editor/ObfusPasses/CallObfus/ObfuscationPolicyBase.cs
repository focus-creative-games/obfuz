using dnlib.DotNet;

namespace Obfuz.ObfusPasses.CallObfus
{
    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedDynamicProxyCallInMethod(MethodDef method);

        public abstract ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method);

        public abstract bool NeedDynamicProxyCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop);
    }
}
