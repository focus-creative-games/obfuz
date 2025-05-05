using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.ConstObfus
{
    public interface IConstObfuscationPolicy
    {
        bool NeedObfuscateMethod(MethodDef method);

        bool NeedObfuscateInt(MethodDef method, int value);

        bool NeedObfuscateLong(MethodDef method, long value);

        bool NeedObfuscateFloat(MethodDef method, float value);

        bool NeedObfuscateDouble(MethodDef method, double value);

        bool NeedObfuscateString(MethodDef method, string value);

        bool NeedObfuscateArray(MethodDef method, byte[] array);
    }
}
