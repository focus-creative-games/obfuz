using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.DynamicProxy
{
    public interface IDynamicProxyPolicy
    {
        bool NeedDynamicProxyCallInMethod(MethodDef method);

        bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir);
    }
}
