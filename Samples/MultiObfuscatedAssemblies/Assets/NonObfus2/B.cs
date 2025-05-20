using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class B
{
    public int x;

    // Start is called before the first frame update
    public void Run(A a, C c, D d)
    {
        x += a.x + 1;
        x += c.x + 2;
        x += d.x + 3;
    }
}
