using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D
{
    public int x;

    public void Run(A a, C c)
    {
        x = a.x + 1;
        x = c.x + 2;
    }
}
