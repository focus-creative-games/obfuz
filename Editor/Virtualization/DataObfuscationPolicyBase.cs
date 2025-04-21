using dnlib.DotNet;

namespace Obfuz.Virtualization
{
    public abstract class DataObfuscationPolicyBase : IDataObfuscationPolicy
    {
        public virtual bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        public virtual bool NeedObfuscateInt(MethodDef method, int value)
        {
            return true;
        }

        public virtual bool NeedObfuscateLong(MethodDef method, long value)
        {
            return true;
        }

        public virtual bool NeedObfuscateFloat(MethodDef method, float value)
        {
            return true;
        }

        public virtual bool NeedObfuscateDouble(MethodDef method, double value)
        {
            return true;
        }

        public virtual bool NeedObfuscateString(MethodDef method, string value)
        {
            return true;
        }
    }
}
