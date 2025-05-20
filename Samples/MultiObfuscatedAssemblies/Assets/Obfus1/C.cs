using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class C
{
    public int x;

    // Start is called before the first frame update
    public void Run(A a)
    {
        x = a.x + 1;
    }
}
