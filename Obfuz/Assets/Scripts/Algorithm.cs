using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Algorithm
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int ComputeHashCode(int a)
    {
        int hash = 17;
        hash = hash * 23 + a.GetHashCode();
        return hash;
    }
}
