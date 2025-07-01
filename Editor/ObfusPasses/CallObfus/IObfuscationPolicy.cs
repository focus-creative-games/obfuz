using dnlib.DotNet;

namespace Obfuz.ObfusPasses.CallObfus
{

    public interface IObfuscationPolicy
    {
        bool NeedObfuscateCallInMethod(MethodDef method);

        bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir);
    }

    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscateCallInMethod(MethodDef method);

        public abstract bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir);
    }
}
