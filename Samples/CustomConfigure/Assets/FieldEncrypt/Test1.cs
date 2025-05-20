using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FieldEncrypt
{
    public class Test1
    {
        public int a;

        [EncryptField]
        public int b;

        public int c;

        public void Add(int x)
        {
            a += x;
            b += x;
            c += x;
        }
    }
}
