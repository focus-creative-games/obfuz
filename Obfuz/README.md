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

// Token: 0x02000003 RID: 3
public class Bootstrap : MonoBehaviour
{
  // Token: 0x06000004 RID: 4 RVA: 0x0000313D File Offset: 0x0000133D
  [RuntimeInitializeOnLoadMethod(2)]
  private static void SetUpStaticSecret()
  {
    Debug.Log("SetUpStaticSecret begin");
    global::$A.$C<$c>.$L = new global::$a.$A(Resources.Load<TextAsset>("Obfuz/defaultStaticSecret").bytes);
    Debug.Log("SetUpStaticSecret end");
  }

  // Token: 0x06000005 RID: 5 RVA: 0x000032E0 File Offset: 0x000014E0
  private void Start()
  {
    global::$a $a = new global::$a();
    int num = global::$e.$A($a, global::$e.$a(global::$d.$A, 8, 117, -2060908889, global::$A.$C<$c>.$d(-1139589574, 85, -452785586)), global::$e.$a(global::$d.$A, 12, 138, -1222258517, global::$A.$C<$c>.$d(-1139589574, 85, -452785586)), global::$A.$C<$c>.$d(-595938299, 185, 132898840));
    global::$e.$b(string.Format(global::$D.$a, num), global::$A.$C<$c>.$d(1718597184, 154, 2114032877));
    int num2 = global::$e.$B($a, num, global::$A.$C<$c>.$d(368894728, 171, -1414000938));
    global::$e.$b(string.Format(global::$D.$A, num2), global::$A.$C<$c>.$d(1718597184, 154, 2114032877));
  }
}

```
