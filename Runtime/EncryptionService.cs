using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public static class EncryptionService
    {
        private static readonly IEncryptor _encryptor = new DefaultEncryptor(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D });

        public static void EncryptBlock(byte[] data, long ops, int salt)
        {
            _encryptor.EncryptBlock(data, ops, salt);
        }

        public static void DecryptBlock(byte[] data, long ops, int salt)
        {
            _encryptor.DecryptBlock(data, ops, salt);
        }
    }
}
