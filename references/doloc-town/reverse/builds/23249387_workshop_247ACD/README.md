# DolocTown Reverse Batch 23249387_workshop_247ACD

生成时间：2026-05-17

## 基线

- Game: Doloc Town
- Branch: workshop
- Steam build: 23249387
- Assembly-CSharp SHA256: `247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367`
- 输入来源：`research/reverse/patch_test_20260517_09605/DolocTown_Data/Managed/Assembly-CSharp.dll.dlkbackup`

注意：`patch_test_20260517_09605/DolocTown_Data/Managed/Assembly-CSharp.dll` 是注入测试后的 DLL，hash 为 `C10C9234667A2B59145F452C15E1D4BA95A3686CBD362CD7C1E0A1CA81304C78`。本批次反编译使用 `.dlkbackup` 官方基线，避免把本项目 patch 逻辑误计入官方 API 地图。

## 产物

- `input/Assembly-CSharp.dll`：本地研究用官方基线副本。
- `input/DO_NOT_DISTRIBUTE_OFFICIAL_DLL.txt`：不要公开分发官方 DLL 的提示。
- `decompiled/Assembly-CSharp/`：`ilspycmd` 导出的完整 C# 项目。
- `metadata/`：Mono.Cecil 抽取的全量元数据表。
- `metadata/summary.json`：全量统计。
- `maps/00_Index.md`：系统 API map 总入口。
- `maps/*.md`：按系统切分的 API map。
- `maps/index/*.csv`：每个系统的原始切片索引。

## 全量统计

- Types: 4792
- Methods: 42873
- Fields: 19519
- Properties: 12293
- Events: 19
- Calls: 173100
- Strings: 20358 occurrences / 7952 unique

## 执行命令

```powershell
dotnet .tools/ilspycmd/8.2.0.7535/tools/net6.0/any/ilspycmd.dll `
  --disable-updatecheck `
  --nested-directories `
  -p `
  -r research/reverse/patch_test_20260517_09605/DolocTown_Data/Managed `
  -o research/reverse/builds/23249387_workshop_247ACD/decompiled/Assembly-CSharp `
  research/reverse/builds/23249387_workshop_247ACD/input/Assembly-CSharp.dll

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/reverse/export_doloctown_reverse_metadata.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/reverse/build_doloctown_api_maps.ps1
```

## 当前结论状态

- `Confirmed`：已从本地反编译/元数据确认。
- `Verified`：需要后续游戏内 mod 运行验证；本批次大多数系统 map 还不是 Verified。
- `Experimental`：可作为 SMAPI 1.x helper 草案输入。
- `Risky`：依赖 private 字段、Harmony patch 时序或官方存档结构。

公开发布时不要分发官方 DLL 或大段反编译源码；应只分发 Runtime/SDK、自写 mod、manifest、文档和提炼后的 API map。
