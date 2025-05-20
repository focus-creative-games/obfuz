using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CallObfus
{
    [ObfuzIgnore]
    public class Test3
    {
        private void CalledTarget()
        {

        }

        public void Run1()
        {
            CalledTarget();
        }

        public void Run2()
        {
            CalledTarget();
        }

        public void Run3()
        {
            CalledTarget();
        }
    }
}

