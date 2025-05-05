using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.ConstObfus
{
    public class RuleBasedObfuscationPolicy : ConstObfuscationPolicyBase
    {
        public override bool NeedObfuscateMethod(MethodDef method)
        {
            return true;
        }

        public override bool NeedObfuscateInt(MethodDef method, int value)
        {
            return value > 10000 || value < -10000;
        }

        public override bool NeedObfuscateLong(MethodDef method, long value)
        {
            return value > 10000 || value < -10000;
        }

        public override bool NeedObfuscateFloat(MethodDef method, float value)
        {
            return true;
        }

        public override bool NeedObfuscateDouble(MethodDef method, double value)
        {
            return true;
        }

        public override bool NeedObfuscateString(MethodDef method, string value)
        {
            return true;
        }

        public override bool NeedObfuscateArray(MethodDef method, byte[] array)
        {
            return true;
        }
    }
}
