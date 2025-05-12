using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class NotObfuscatedMethodWhiteList
    {

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == "Obfuz.Runtime")
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
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            //if (type.Name.StartsWith("$Obfuz$"))
            //{
            //    continue;
            //}
            if (IsInWhiteList(type.Module))
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
