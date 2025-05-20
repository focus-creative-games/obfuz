using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SymbolObfus
{
    [ObfuzIgnore]
    public class Test2
    {
        public class NestedTypeInTest1
        {
            public int x;
        }

        public int x;

        public int Value { get; set; }

        public void Foo()
        {

        }

        public event System.Action<int> OnValueChanged;
    }

}
