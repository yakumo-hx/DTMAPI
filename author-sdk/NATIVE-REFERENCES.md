# 原生编译引用

作者可以直接使用本机 Unity、Harmony、BepInEx 和游戏引用；目标程序集保持 `netstandard2.0`，原生引用设置 `Private=false`。CodeMod 不分 Strict/Advanced 准入等级。

游戏程序集含内部类型或不适合标准编译器的表面时，可选用：

```text
dtmapi-author native-references GAME/DolocTown_Data/Managed/Assembly-CSharp.dll GAME/DolocTown_Data/Managed/UnityEngine.CoreModule.dll --output obj/native-references
```

也可独立构建 `src/DTMAPI.NativeReferences`，以 `输出目录 DLL...` 调用。转换器只生成编译引用；不产生 Runtime 准入证明，不要求 policy registry、必需成员清单或发行准备。输出含 NativeReferences.props 时可普通导入，或用 Reference 显式引用 DLL。

引用来自作者自行选择的本机安装。转换产物放 `obj` 或其他可重建目录，不作为产品运行代码打包。游戏升级后按实际编译错误或行为差异更新引用；不靠全目录哈希推导兼容。
