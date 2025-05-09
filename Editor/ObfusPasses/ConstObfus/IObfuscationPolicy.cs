using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.ConstObfus
{
    public struct ConstCachePolicy
    {
        public bool cacheConstInLoop;
        public bool cacheConstNotInLoop;
        public bool cacheStringInLoop;
        public bool cacheStringNotInLoop;
    }

    public interface IObfuscationPolicy
    {
        bool NeedObfuscateMethod(MethodDef method);

        ConstCachePolicy GetMethodConstCachePolicy(MethodDef method);

        bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value);

        bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value);

        bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value);

        bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value);

        bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value);

        bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array);
    }
}
