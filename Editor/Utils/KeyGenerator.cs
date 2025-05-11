using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public static class KeyGenerator
    {
        public static byte[] GenerateKey(string initialString, int keyLength)
        {
            byte[] initialBytes = Encoding.UTF8.GetBytes(initialString);
            using var sha512 = SHA512.Create();
            byte[] hash = sha512.ComputeHash(initialBytes);
            byte[] key = new byte[keyLength];
            int bytesCopied = 0;
            while (bytesCopied < key.Length)
            {
                if (bytesCopied > 0)
                {
                    // 再次哈希之前的哈希值以生成更多数据
                    hash = sha512.ComputeHash(hash);
                }
                int bytesToCopy = Math.Min(hash.Length, key.Length - bytesCopied);
                Buffer.BlockCopy(hash, 0, key, bytesCopied, bytesToCopy);
                bytesCopied += bytesToCopy;
            }
            return key;
        }

        public static int[] ConvertToIntKey(byte[] key)
        {
            Assert.AreEqual(0, key.Length % 4);
            int align4Length = key.Length / 4;
            int[] intKey = new int[align4Length];
            Buffer.BlockCopy(key, 0, intKey, 0, key.Length);
            return intKey;
        }
    }
}
