using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.DynamicProxy
{
    public class ConfigDynamicProxyPolicy : DynamicProxyPolicyBase
    {
        public override bool NeedDynamicProxyCallInMethod(MethodDef method)
        {
            return true;
        }

        public override bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir)
        {
            return true;
        }
    }
}
