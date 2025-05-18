using Obfuz;
using Obfuz.EncryptionVM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bootstrap : MonoBehaviour
{
    // [ObfuzIgnore]指示Obfuz不要混淆这个函数
    // 初始化EncryptionService后被混淆的代码才能正常运行，
    // 因此尽可能地早地初始化它。
    [ObfuzIgnore]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void SetUpStaticSecretKey()
    {
        Debug.Log("SetUpStaticSecret begin");
        EncryptionService<DefaultStaticEncryptionScope>.Encryptor = new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
        Debug.Log("SetUpStaticSecret end");
    }

    int Add(int a, int b)
    {
        return a + b + 1;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello, Obfuz");
        int a = Add(10, 20);
        Debug.Log($"a = {a}");
    }
}
