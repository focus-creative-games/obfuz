using dnlib.DotNet;

namespace Obfuz.ObfusPasses.ConstObfus.Policies
{
    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscateMethod(MethodDef method);
        public abstract ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);
        public abstract bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);
        public abstract bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);
        public abstract bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);
        public abstract bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);
        public abstract bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);
        public abstract bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }
}
