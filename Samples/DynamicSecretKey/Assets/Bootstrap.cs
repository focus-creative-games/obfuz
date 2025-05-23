using Obfuz;
using Obfuz.EncryptionVM;
using UnityEngine;


public class Bootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void SetUpStaticSecretKey()
    {
        Debug.Log("SetUpStaticSecret begin");
        EncryptionService<DefaultStaticEncryptionScope>.Encryptor = new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
        Debug.Log("SetUpStaticSecret end");
    }

    private static void SetUpDynamicSecret()
    {
        Debug.Log("SetUpDynamicSecret begin");
        EncryptionService<DefaultDynamicEncryptionScope>.Encryptor = new GeneratedEncryptionVirtualMachine(Resources.Load<TextAsset>("Obfuz/defaultDynamicSecretKey").bytes);
        Debug.Log("SetUpDynamicSecret end");
    }


    void Start()
    {
        // 延迟加载，在使用HotUpdate程序集代码前才加载动态密钥。
        // 如果项目用到了热更新，一般来说在热更新完成后，加载
        // 热更新代码前才加载动态密钥。
        SetUpDynamicSecret();
        this.gameObject.AddComponent<Entry>();
    }
}
