using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public static class MetaUtil
    {
        public static string GetModuleNameWithoutExt(string moduleName)
        {
            return Path.GetFileNameWithoutExtension(moduleName);
        }
    }
}
