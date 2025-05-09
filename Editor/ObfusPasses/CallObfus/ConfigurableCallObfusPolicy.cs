using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class ConfigurableCallObfusPolicy : CallObfusPolicyBase
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
            if (typeDef != null)
            { 
                // need configurable
                if (typeDef.Module.IsCoreLibraryModule == true)
                {
                    return false;
                }
                if (typeDef.IsDelegate)
                    return false;
            }
            // doesn't proxy call if the method is a constructor
            if (method.Name == ".ctor")
            {
                return false;
            }
            // special handle
            // don't proxy call for List<T>.Enumerator GetEnumerator()
            if (method.Name == "GetEnumerator")
            {
                return false;
            }
            return true;
        }
    }
}
