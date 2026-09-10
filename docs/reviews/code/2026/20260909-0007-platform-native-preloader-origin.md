# PN-023 — 内存加载宿主的原始来源

- Date: `2026-09-09`
- Status: `accepted with explicit limits`
- Owner: [PN-023 Update](../../../updates/2026/20260909-0015-platform-m3-composition.md)
- Decision: 使用 Bootstrap 在受管发现之前捕获的 BepInEx 原始路径；保持驻留冲突、身份、MVID、当前磁盘摘要和实际必需签名检查。不是允许任意无路径程序集。

## 新事实与原因

172128/172443 的 Rowan 在首次直接使用 UnityEngine.Object 比较运算符时，触发 UnityEngine.CoreModule 必需签名校验。该模块由 BepInEx 5.4.23.5 预加载修补，从内存载入；CLR Location 为空、module 为 data-*。之前 Cedar/Pine 使用 object 局部变量比较，不产生该 Unity MemberRef，因而未覆盖此边界。空路径直接进入 Path.GetFullPath 是本轮发现错误的原因，与作者共享库角色无关。

当前 BepInEx [AssemblyPatcher](https://github.com/BepInEx/BepInEx/blob/v5.4.23.5/BepInEx.Preloader/Patching/AssemblyPatcher.cs) 记录原始路径，[UnityPatches](https://github.com/BepInEx/BepInEx/blob/v5.4.23.5/BepInEx.Preloader/RuntimeFixes/UnityPatches.cs) 原本也使用该映射提供 Location/CodeBase。这里只读读取已加载的对应预加载器映射，不复制其实现、不修改宿主、不添加第六个运行时 DLL。

## 边界与验证决定

Bootstrap 必须找到唯一、位于预期 BepInEx/core 的预加载程序集；仅接收游戏 Managed 范围内、当前已驻留且无路径的宿主。Core 绑定原始文件身份/MVID/摘要到精确 Assembly 对象；重新绑定不能接受新字节。后续仍要求唯一驻留身份、声明路径一致、原始文件未变和驻留必需签名存在。映射缺失/更改、版本不支持或未知内存加载拒绝，不能降级为仅按 simple name 放行。

原始磁盘文件的 hash 不是修补后内存方法体的 hash。诊断明确使用 native-host-preloader-patched，修补内容由 loader 拥有；本格式不承诺隔离恶意 loader/其他任意原生代码。MVID 一致不能单独证明未经修补，不以它替代来源映射。既有原生契约的包绑定/源选择不改变。

针对此原因增加公开打包语料：未知内存来源拒绝、不同来源拒绝、正确预观察接受并提示、磁盘替换及重新绑定拒绝。该 focused suite 已通过；实际组合加载与完整收口继续由 Update 的新证据维护。
