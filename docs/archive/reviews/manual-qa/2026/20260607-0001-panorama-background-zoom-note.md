# 20260607-0001 - Panorama Background And Zoom Note

## 手测记录头

- 时间：2026-06-07 00:00:47 +08:00
- 来源：用户询问小神增强包全景照相机与 DTMAPI Zoom 的差异，并提供 DTMAPI Zoom 大视野截图。
- 范围：经验总结 / 后续统一升级 goal 素材；不生成 goal，不实现代码。
- 禁止事项：不复制小神增强包代码；不做新的相机 mod；不把普通 Zoom 扩成全景照相机。

## 结论

DTMAPI 当前 ZoomMod 已覆盖玩家需要的实时大视野能力，主要缺口是：农场大视野时背景/雾层没有随相机缩放，导致背景像中间小框。

小神增强包的全景照相机不是普通 Zoom。只读逆向显示它会 fit 当前房间、隐藏玩家/无人机/摩托、补偿背景和景深雾层、延迟进入官方 `PhotoState`，并在退出时恢复相机、背景和可见对象。

## 需要补充的 API

- 后续 DTMAPI 可以补充 experimental `IPanoramaCameraApi` / `IPhotoCameraApi`。
- API 目标是给未来全景/拍照类 mod 使用，负责进入/退出全景模式、fit 房间、截图、隐藏角色/载具、恢复状态、地牢邻近房间策略。
- 本轮不实现全景相机 mod，也不要求 ZoomMod 使用全景 API。

## Zoom 需要补强的最小点

ZoomMod / `ICameraZoomApi` 后续只需要增加背景补偿：

- 缩放 `DolocAPI.envBackgroundEx` 的 transform scale。
- 反射读取 `EnvCovariantController._depthFogController`，同步缩放景深雾层 transform scale。
- 缩放倍率应基于 `currentOrthographicSize / vanillaOrthographicSize`。
- 退出存档、返回标题、恢复 1x、关闭 mod 时必须恢复原始 scale。
- 不改变 UI scale，不进入 `PhotoState`，不隐藏玩家/无人机/摩托。

## 参考路径

- 小神增强包临时只读分析：`DolocPlus.Functions.CameraRescaler`
- 小神增强包临时只读分析：`DolocPlus.Features.PanoramaViewController`
- 小神增强包临时只读分析：`DolocPlus.Features.BackGroundViewController`
- 游戏原生：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/CameraController.cs`
- 游戏原生：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/BackgroundRenderer.cs`
- 游戏原生：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Dungeon.cs`
- 游戏原生：`references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/PhotoState.cs`

## 后续验收点

- 农场 4x 大视野背景不再显示为中间小框。
- 景深雾层覆盖范围随相机缩放，不出现明显断层。
- 室内背景行为不被错误放大或错误显示。
- 退出存档、返回标题、重启游戏后背景/雾层 scale 无残留污染。
- Zoom 仍是普通游玩视野 mod，不进入全景拍照状态。
