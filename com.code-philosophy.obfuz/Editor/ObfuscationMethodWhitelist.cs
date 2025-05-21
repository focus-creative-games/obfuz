using dnlib.DotNet;
using Obfuz.Editor;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class ObfuscationMethodWhitelist
    {

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == "Obfuz.Runtime")
            {
                return true;
            }
            //if (MetaUtil.HasObfuzIgnoreScope(module))
            //{
            //    return true;
            //}
            return false;
        }

        public bool IsInWhiteList(MethodDef method)
        {
            if (IsInWhiteList(method.DeclaringType))
            {
                return true;
            }
            if (method.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(method, method.DeclaringType, ObfuzScope.MethodBody))
            {
                return true;
            }
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            if (type.Name.StartsWith(ConstValues.ObfuzInternalSymbolNamePrefix))
            {
                return true;
            }
            if (IsInWhiteList(type.Module))
            {
                return true;
            }
            if (MetaUtil.HasSelfOrInheritObfuzIgnoreScope(type, type.DeclaringType, ObfuzScope.TypeName))
            {
                return true;
            }
            //if (type.DeclaringType != null && IsInWhiteList(type.DeclaringType))
            //{
            //    return true;
            //}
            if (type.FullName == "Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine")
            {
                return true;
            }
            return false;
        }
    }
}
