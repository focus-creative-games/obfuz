using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class All : MonoBehaviour
{
    public int x;

    public void Run(A a, B b, C c, D d)
    {
        x += a.x + 1;
        x += b.x + 2;
        x += c.x + 3;
        x += d.x + 4;
    }
}
