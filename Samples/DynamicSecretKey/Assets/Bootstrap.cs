using Obfuz;
using Obfuz.EncryptionVM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    private static void SetUpDynamicSecret()
    {
        EncryptionService<DefaultDynamicEncryptionScope>.Encryptor = new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultDynamicSecretKey").bytes);
        // 设置其他动态EncryptionScope的Encryptor
        // ...
    }


    // Start is called before the first frame update
    void Start()
    {
        // 在完成热更之后，加载热更DLL之前，加载Obfuz的动态密钥
        SetUpDynamicSecret();
#if UNITY_EDITOR
        Assembly ass = AppDomain.CurrentDomain.GetAssemblies().First(ass => ass.GetName().Name == "HotUpdate");
#else
        Assembly ass = Assembly.Load(File.ReadAllBytes($"{Application.streamingAssetsPath}/HotUpdate.dll.bytes"));
#endif
        Type entry = ass.GetType("Entry");
        this.gameObject.AddComponent(entry);
    }
}
