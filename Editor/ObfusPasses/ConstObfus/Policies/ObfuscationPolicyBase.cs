using dnlib.DotNet;

namespace Obfuz.ObfusPasses.ConstObfus.Policies
{
    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscateMethod(MethodDef method);
        public abstract bool NeedObfuscateDouble(MethodDef method, double value);
        public abstract bool NeedObfuscateFloat(MethodDef method, float value);
        public abstract bool NeedObfuscateInt(MethodDef method, int value);
        public abstract bool NeedObfuscateLong(MethodDef method, long value);
        public abstract bool NeedObfuscateString(MethodDef method, string value);
        public abstract bool NeedObfuscateArray(MethodDef method, byte[] array);
    }
}
