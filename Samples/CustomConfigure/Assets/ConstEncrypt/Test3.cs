using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConstEncrypt
{
    [ObfuzIgnore]
    public class Test3
    {
        public int Sum1(int a)
        {
            return a + 10;
        }

        public int Sum2(int a)
        {
            return a + 10000;
        }

        public int Sum3(int a)
        {
            return a + 10000;
        }

        public string GetShortName()
        {
            return "abc";
        }

        public string GetLongName()
        {
            return "abcdefghijklmno";
        }
    }


}