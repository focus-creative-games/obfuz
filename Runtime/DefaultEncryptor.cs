using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public class DefaultEncryptor : IEncryptor
    {
        private readonly byte[] _key;

        public DefaultEncryptor(byte[] key)
        {
            _key = key;
        }

        public void EncryptBlock(byte[] data, long ops, int salt)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)(_key[i % _key.Length] ^ salt);
            }
        }

        public void DecryptBlock(byte[] data, long ops, int salt)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)(_key[i % _key.Length] ^ salt);
            }
        }
    }
}
