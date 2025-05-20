using Obfuz;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CallObfus
{
    public class Test4
    {
        private void CalledTarget()
        {

        }

        [ObfuzIgnore]
        public void Run1()
        {
            CalledTarget();
        }

        [ObfuzIgnore]
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

