using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.MemEncrypt
{
    public interface IEncryptionPolicy
    {
        bool NeedEncrypt(FieldDef field);
    }
}
