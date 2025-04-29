using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.DynamicProxy
{
    public class ConfigProxyCallPolicy : ProxyCallPolicyBase
    {
        public override bool NeedDynamicProxyCallInMethod(MethodDef method)
        {
            return true;
        }

        public override bool NeedDynamicProxyCalledMethod(IMethod method, bool callVir)
        {
            
            ITypeDefOrRef declaringType = method.DeclaringType;
            TypeDef typeDef = declaringType.ResolveTypeDef();
            // doesn't proxy call if the method is a delegate
            if (typeDef != null && typeDef.IsDelegate)
            {
                return false;
            }
            if (method.Name == ".ctor")
            {
                return false;
            }
            return true;
        }
    }
}
