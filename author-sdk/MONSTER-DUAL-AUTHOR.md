# 同一原版蜂的两位护甲作者

Runtime 0.7.5 候选内置怪物模块。Pack 声明 bee 的 defense=18，CodeMod 声明同一字段为 12；胜者由进程固定加载顺位决定，不取决于声明时间或数值大小。字段范围见[覆盖契约](MONSTER-NATIVE-OVERRIDES.md)。

套件 `examples/OverridePack/package` 是普通内容目录，保留 v4 项目与 Catalog；`examples/OverrideCode` 是普通 netstandard2.0 工程。Abstractions 和 MonsterContent 引用使用 Private=false，由 Runtime 供给，不复制进产品。文件和 API 进入同一服务与作用域。

```powershell
./tools/monster-editor/start.ps1
./tools/scripts/build-mod-project.ps1 -ProjectPath ./examples/OverrideCode/Samples.MonsterOverrideCode.csproj -ReferencesRoot ./contracts -OutputRoot E:/MyMods/OverrideCode-build
```

编辑器打开 `examples/OverridePack/package/PackData/authoring/project.dtmonster.json` 并导入目录；代码修改 ModEntry.cs 的 Defense。导出采用 `PackData/content.json`。安装 Runtime 后，将两个作品目录放入官方 MODS 并启用，调整顺序后冷启动。

诊断列出 requested、effective、baseline、winner、字段状态和 DeclarationOrder。Pack 在 Code 之前时，Code 的 12 生效，Pack 为 Shadowed；释放 Code 注册后恢复 Pack 的 18，再释放 Pack 后恢复原生值。每个注册独立撤回；实际实例的冷却和已发射效果按原生字段读取时点处理。无需先取得 RequestModule，保留预热租约也不能阻止定义自身被撤回。
