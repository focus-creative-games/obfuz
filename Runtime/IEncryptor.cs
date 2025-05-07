using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IEncryptor
    {
        void EncryptBlock(byte[] data, long ops, int salt);
        void DecryptBlock(byte[] data, long ops, int salt);
    }
}
