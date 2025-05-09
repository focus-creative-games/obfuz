using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CallObfus
{
    public interface ICallObfusPolicy
    {
        bool NeedDynamicProxyCallInMethod(MethodDef method);

        bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir);
    }
}
