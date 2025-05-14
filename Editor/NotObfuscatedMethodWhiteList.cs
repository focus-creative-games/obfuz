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
    public class NotObfuscatedMethodWhiteList
    {
        private bool ShouldBeIgnoredByCustomAttribute(IHasCustomAttribute obj)
        {
            return MetaUtil.HasObfuzIgnoreAttribute(obj) || MetaUtil.HasCompilerGeneratedAttribute(obj);
        }

        public bool IsInWhiteList(ModuleDef module)
        {
            string modName = module.Assembly.Name;
            if (modName == "Obfuz.Runtime")
            {
                return true;
            }
            if (ShouldBeIgnoredByCustomAttribute(module))
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
            if (method.Name.StartsWith(ConstValues.ObfuzMetadataNamePrefix))
            {
                return true;
            }
            if (ShouldBeIgnoredByCustomAttribute(method))
            {
                return true;
            }
            return false;
        }

        public bool IsInWhiteList(TypeDef type)
        {
            if (type.Name.StartsWith(ConstValues.ObfuzMetadataNamePrefix))
            {
                return true;
            }
            if (IsInWhiteList(type.Module))
            {
                return true;
            }
            if (ShouldBeIgnoredByCustomAttribute(type))
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
