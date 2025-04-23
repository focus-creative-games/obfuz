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

        public void EncryptBytes(byte[] data, int minorSecret)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)(_key[i % _key.Length] ^ minorSecret);
            }
        }

        public void DecryptBytes(byte[] data, int minorSecret)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= (byte)(_key[i % _key.Length] ^ minorSecret);
            }
        }
    }
}
