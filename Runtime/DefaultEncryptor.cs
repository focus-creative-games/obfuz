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

        public int Encrypt(int value, int opts, int salt)
        {
            return value;
        }

        public int Decrypt(int value, int opts, int salt)
        {
            return value;
        }

        public long Encrypt(long value, int opts, int salt)
        {
            return value;
        }

        public long Decrypt(long value, int opts, int salt)
        {
            return value;
        }

        public float Encrypt(float value, int opts, int salt)
        {
            return value;
        }

        public float Decrypt(float value, int opts, int salt)
        {
            return value;
        }

        public double Encrypt(double value, int opts, int salt)
        {
            return value;
        }

        public double Decrypt(double value, int opts, int salt)
        {
            return value;
        }

        public int[] Encrypt(byte[] value, int offset, int length, int opts, int salt)
        {
            var intArr = new int[(length + 3) / 4];
            Buffer.BlockCopy(value, offset, intArr, 0, length);
            return intArr;
        }

        public byte[] Decrypt(int[] value, int offset, int byteLength, int ops, int salt)
        {
            byte[] byteArr = new byte[byteLength];
            Buffer.BlockCopy(value, 0, byteArr, 0, byteLength);
            return byteArr;
        }

        public int[] Encrypt(string value, int ops, int salt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public string DecryptString(int[] value, int offset, int stringBytesLength, int ops, int salt)
        {
            byte[] bytes = new byte[stringBytesLength];
            Buffer.BlockCopy(value, 0, bytes, 0, stringBytesLength);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
