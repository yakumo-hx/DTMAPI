# 清理失败后继续并重试

这是源码示例，使用当前 SDK 创建普通工程后替换入口即可构建。示例只改变自身的受管标量，故意让第一次恢复失败，不修改游戏动画或安装 Harmony Hook。

在 SDK 环境中执行，`SDK_ROOT` 是解压目录，`WORK` 是新的作者工程目录：

```powershell
dtmapi-author new codemod WORK --id Samples.OwnedCleanup --name "Owned cleanup example" --author Samples --api-target 0.7.0
Copy-Item SDK_ROOT/examples/OwnedCleanup/ModEntry.cs WORK/src/ModEntry.cs
dtmapi-author restore WORK --json
dtmapi-author pack WORK --json
```

用正常作者安装／会话流程加载包。在标题页 Status 的命令入口执行 `Samples.OwnedCleanup/close`：应得到 `hooks=Disposed; motion=Failed`，失败写入 Runtime 诊断。再执行 `Samples.OwnedCleanup/retry`：应得到两项 `Disposed`、`rate=1`、`restoreAttempts=2`。继续重试不增加次数。F9 使用同一操作，但命令测试不证明实体按键。

| 平台责任 | 产品责任 |
| --- | --- |
| owned resource 的线程、范围、倒序关闭、失败保留和重试状态 | 提供准确的恢复动作；停用残留回调；恢复自身曾改变的原生值 |
| owner 关闭时撤回事件、输入、命令等注册 | 原生 Hook 的准确 owner、嵌套操作状态、跨实例隔离和失败重试条件 |
| 关闭失败进入诊断和最终 owner 清理结果 | 不能吞掉错误或在未恢复时丢弃原生对象／句柄 |

AF 的清理需要单项失败后仍释放其他对象；AS 还需恢复动画并确保残留回调失活。这里将独立恢复动作交给已有 owned resources，不在产品中再维护一份失败队列。实际产品中彼此依赖的动作仍可由同一个 `IDisposable` 聚合，逐项尝试并在末尾抛出准确错误；R1 的 scope／实例保护属于产品原生适配。

`Dispose` 不保证已经成功，应读取 token 的 `Status`。失败保持 `Failed`，同一 token 可以重试；`Unregister` 只交还所有权，不负责恢复。owner 已关闭后不能重新获取服务或靠退订来清理；持有的旧失败 token／Runtime 清理重试仍绑定旧实例。发生共享 Hook 清理失败时，产品应阻止另一实例接管同一物理 owner，直到恢复完成。
