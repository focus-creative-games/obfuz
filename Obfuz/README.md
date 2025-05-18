# Obfuz

这是一个极简的Obfuz示例项目。

## 配置

点击 `Obfuz/Settings...`菜单打开ObfuzSettings页面。
可以看到`Assembly Settings`里已经添加了两个混淆程序集：Assembly-CSharp和Obfuz.Runtime（没错，Obfuz自身也是可混淆的）。

## 构建

打开`Build Settings`，运行`Build`或`Build and Run`即可。

## 查看混淆文件

混淆后的文件在`Library/Obfuz/{buildTarget}/ObfuscatedAssemblies目录下。

使用IlSpy打开`Assembly-CSharp.dll`，混淆后的Bootstrap类代码变成这样：

```csharp

using System;
using $a;
using $A;
using UnityEngine;

// Token: 0x02000002 RID: 2
public class Bootstrap : MonoBehaviour
{
  // Token: 0x06000001 RID: 1 RVA: 0x000030F0 File Offset: 0x000012F0
  [RuntimeInitializeOnLoadMethod(2)]
  private static void SetUpStaticSecret()
  {
    Debug.Log("SetUpStaticSecret begin");
    global::$A.$C<$c>.$L = new $a.$A(Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey").bytes);
    Debug.Log("SetUpStaticSecret end");
  }

  // Token: 0x06000002 RID: 2 RVA: 0x0000311F File Offset: 0x0000131F
  public int $a(int 1, int 1)
  {
    return 1 + 1;
  }

  // Token: 0x06000003 RID: 3 RVA: 0x00003204 File Offset: 0x00001404
  private void Start()
  {
    int num = global::$e.$A(this, global::$e.$a(global::$d.$A, 0, 117, -2060908889, global::$A.$C<$c>.$d(-1139589574, 85, -452785586)), global::$e.$a(global::$d.$A, 4, 138, -1222258517, global::$A.$C<$c>.$d(-1139589574, 85, -452785586)), global::$A.$C<$c>.$d(1757957431, 242, 760404455));
    global::$e.$b(string.Format(global::$D.$a, num), global::$A.$C<$c>.$d(1718597184, 154, 2114032877));
  }
}



```
