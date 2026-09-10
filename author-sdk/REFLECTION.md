# 精确反射 V1 候选

当前接口属于内部 API/SDK/Runtime 0.6.2 候选，已进入普通 SDK 构建路径；不是公开发行。首次公开版本须等 M3/首发验收。旧 API 0.5.5 及历史 M2 0.7 快照无这些类型；PN-021 原 0.8 隔离候选保持历史身份。

解压当前 SDK，使用公开 `new codemod`、`build`、`pack`、`install-local` 和 `withdraw`。当前默认目标为 0.7.0；可显式选择保留的 0.6.2。版本范围见 [目标目录](target-catalog.json)，采用前查看 [API 状态](API-STATUS.md) 和 [迁移](MIGRATION.md)。

在 `Entry(IDtmHelper helper)` 的运行线程取得服务。服务取得后，对普通托管对象的查找和调用可以在调用者线程进行；不会自动调度到游戏线程。访问 Unity/原生对象的线程与存活责任仍由作者承担。下面访问作者自己的对象：

```csharp
IReflectionHelper reflection = helper.GetReflection();
var state = new AuthorState();
using (IReflectedField<int> value = reflection.GetField<int>(state, "value"))
{
    helper.Monitor.Log(value.GetValue().ToString());
    if (value.CanWrite) value.SetValue(8);
}
using (IReflectedMethod method = reflection.GetMethod(state, "Describe", new[] { typeof(int) }))
    helper.Monitor.Log(method.Invoke<string>(3));

// Place this type alongside your ModEntry class.
sealed class AuthorState
{
    private int value = 7;
    private string Describe(int amount) => (value + amount).ToString();
}
```

字段/属性在绑定时检查声明类型能否赋给 `T`；写入时检查实际值，方法调用前检查参数与声明返回类型。不会做数值、字符串、枚举隐式转换，也不会展开可选参数或 params。静态成员用 `GetStaticField`、`GetStaticProperty`、`GetStaticMethod`。

`TryGet…` 只在精确成员不存在时返回 false，out 为 null；成员返回 null 则是正常成功。错类型是 `InvalidCastException`，参数错误是 `ArgumentException`，多个候选是 `AmbiguousMatchException`。泛型方法、ref/out、索引器、只写属性、值类型实例修改、readonly/const 写入和非 void 方法走非泛型 Invoke 抛 `NotSupportedException`。目标异常保留在 `TargetInvocationException.InnerException` 及原始堆栈中。

及时 Dispose 包装器会释放其目标。owner 失败、停用或 Runtime 关闭后，保留的服务/包装器拒绝新调用；仍存活的包装器会被清除目标，即使不再调用它也能回收。已获准开始的调用使用局部引用完成，关闭不回滚它的副作用。缓存只留有界元数据，owner 用弱登记跟踪包装器。

Strict 可反射作者自己的托管对象，这不构成安全沙箱，也不保证原生字段写入具有正确玩法语义。共享契约 DLL、自助 Advanced 和 native 引用属于另外的 M3 工作包。
