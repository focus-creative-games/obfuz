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
        private bool HasObfuzIgnoreScope(IHasCustomAttribute obj, ObfuzScope targetScope)
        {
            ObfuzScope? objScope = MetaUtil.GetObfuzIgnoreScope(obj);
            if (objScope == null)
            {
                return false;
            }
            return (objScope & targetScope) != 0;   
        }

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == "Obfuz.Runtime")
            {
                return true;
            }
            if (HasObfuzIgnoreScope(module, ObfuzScope.Self))
            {
                return true;
            }
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
            if (HasObfuzIgnoreScope(method, ObfuzScope.Self))
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
            if (MetaUtil.HasObfuzIgnoreAttributeInSelfOrParent(type))
            {
                return true;
            }
            if (type.DeclaringType != null && IsInWhiteList(type.DeclaringType))
            {
                return true;
            }
            if (type.FullName == "Obfuz.EncryptionVM.GeneratedEncryptionVirtualMachine")
            {
                return true;
            }
            return false;
        }
    }
}
