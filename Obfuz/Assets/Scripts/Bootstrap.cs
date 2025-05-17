using Obfuz;
using Obfuz.EncryptionVM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bootstrap : MonoBehaviour
{
    [ObfuzIgnore]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void SetUpStaticSecret()
    {
        Debug.Log("SetUpStaticSecret begin");
        EncryptionService<DefaultStaticEncryptionScope>.Encryptor = new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
        Debug.Log("SetUpStaticSecret end");
    }

    // Start is called before the first frame update
    void Start()
    {
        var c = new Algorithm();
        int a = c.Add(100, 200);
        Debug.Log($"a = {a}");

        int b = c.ComputeHashCode(a);
        Debug.Log($"b = {b}");
    }
}
