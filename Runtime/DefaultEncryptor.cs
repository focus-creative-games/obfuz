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

        public byte[] Encrypt(byte[] value, int offset, int length, int opts, int salt)
        {
            int align4Length = (length + 3) & ~3;
            var encryptedBytes = new byte[align4Length];
            Buffer.BlockCopy(value, offset, encryptedBytes, 0, length);
            return encryptedBytes;
        }

        public byte[] Decrypt(byte[] value, int offset, int length, int ops, int salt)
        {
            byte[] byteArr = new byte[length];
            Buffer.BlockCopy(value, 0, byteArr, 0, length);
            return byteArr;
        }

        public byte[] Encrypt(string value, int ops, int salt)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Encrypt(bytes, 0, bytes.Length, ops, salt);
        }

        public string DecryptString(byte[] value, int offset, int length, int ops, int salt)
        {
            byte[] bytes = new byte[length];
            Buffer.BlockCopy(value, 0, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
