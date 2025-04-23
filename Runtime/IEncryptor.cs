using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IEncryptor
    {
        void EncryptBytes(byte[] data, int minorSecret);
        void DecryptBytes(byte[] data, int minorSecret);
    }
}
