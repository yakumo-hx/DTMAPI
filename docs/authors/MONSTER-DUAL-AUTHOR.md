# 同一原版蜂的两位护甲作者

Pack 声明 bee 的 defense=18，CodeMod 声明同一字段为 12；胜者由进程固定加载顺位决定，不取决于声明时间或数值大小。字段范围见[覆盖契约](MONSTER-NATIVE-OVERRIDES.md)。

内容包在 `PackData/content.json` 声明字段，代码 Mod 通过 `RegisterOverride` 注册同一字段。安装 Runtime 后，将两个 Mod 放入官方 MODS 并启用，调整顺序后重启游戏。代码 Mod 的 API 引用设为 `Private=false`，由 Runtime 提供。

诊断列出 requested、effective、baseline、winner、字段状态和 DeclarationOrder。Pack 在 Code 之前时，Code 的 12 生效，Pack 为 Shadowed；释放 Code 注册后恢复 Pack 的 18，再释放 Pack 后恢复原生值。每个注册独立撤回；实际实例的冷却和已发射效果按原生字段读取时点处理。无需先取得 RequestModule，保留预热租约也不能阻止定义自身被撤回。
