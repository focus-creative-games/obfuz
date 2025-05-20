using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SymbolObfus
{
    public class Test3
    {

        [ObfuzIgnore]
        public class NestedTypeInTest1
        {
            public int x;
        }

        public class NestedTypeInTest2
        {
            public int x;
        }

        [ObfuzIgnore]
        public int x1;

        public int x2;

        [ObfuzIgnore]
        public int Value1 { get; set; }

        public int Value2 { get; set; }

        [ObfuzIgnore]
        public void Foo()
        {

        }

        public void Bar()
        {
        }

        [ObfuzIgnore]
        public event System.Action<int> OnValueChanged1;

        public event System.Action<int> OnValueChanged2;
    }

}
