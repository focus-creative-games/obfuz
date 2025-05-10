using dnlib.DotNet;

namespace Obfuz.ObfusPasses.CallObfus
{
    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscateCallInMethod(MethodDef method);

        public abstract ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method);

        public abstract bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop);
    }
}
