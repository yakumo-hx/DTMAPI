# 20260607-0003 - CameraZoom 0.4.2 Manual Failure Review

## 手测记录头

- 时间：2026-06-07 08:50:05 +08:00
- 来源：用户手测反馈；上一轮 `0.4.2` CameraZoom API rebuild 标记完成后，用户报告大视野实际不可用。
- 范围：代码级审查 / root-cause review；本文件不生成实现 goal，不修改 runtime。
- 禁止事项：不把 `GAME-SMOKE/20260607-072228` 当作用户验收通过；不继续用静态截图证明可玩性；不先写修复代码。
- 审查记录：本文件。

## 问题 1：Zoom 后相机不再以人物为中心，而像固定场景中心

原始反馈：

- 放大后不再以人物为中心变化，而是固定场景为中心。
- 人走动会一段距离一段距离刷新背景。

审查记录：

- 用户确认事实：玩家实际游玩时，4x 大视野不是跟随人物的连续相机，而是接近固定场景中心/分段刷新。
- 截图/日志观察：`GAME-SMOKE/20260607-072228` 只记录 4x 静态应用和静态截图；没有移动输入、人物位移、camera 跟随连续性、垂直高度变化或长时间保持 4x 的证据。
- 代码/文档事实：
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2378` 每帧调用 `RefreshCameraZoomForRuntime()`，进而反复调用 `ApplyCameraZoomTarget("runtime refresh")`。
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2446` 在每次 apply 中调用 `RefreshNativeCameraControllerAndPosition()`。
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2715` 的 `RefreshNativeCameraControllerAndPosition()` 调 `CameraController.RefreshResolution()`，然后用 `DolocAPI.AgentPosition` 调 `CameraController.SetPosition(...)`。
  - 原生 `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/CameraController.cs:266` 的 `RefreshResolution()` 根据当前 `mainCamera.orthographicSize` 重算 `camSize`，再调用 `SetRoomRange(scenePosition, sceneSize)`。
  - 原生 `CameraController.SetRoomRange(...)` 在 `CameraController.cs:300` 只对 `sceneSize == camSize` 做固定中心处理；没有处理 `camSize > sceneSize`。当 4x 后 `camSize` 大于房间/农场可滚动范围，`xRange` / `yRange` 可能反转或异常。
  - 原生 `CameraController.SetPosition(...)` 在 `CameraController.cs:332` 依赖 `xRange` / `yRange` clamp。范围异常时，clamp 结果不再代表“以人物为中心”。
  - 原生 `AgentControllerState.OnFixedUpdate(...)` 在 `AgentControllerState.cs:406` 调 `cameraController.UpdateCamPosition(dt)`，相机连续跟随本来由原生 controller 驱动，而不是 DTMAPI 每帧强行 `SetPosition`。
- Codex 推断：
  - 0.4.2 把“放大视野”实现为放大 `orthographicSize` + 刷新 `CameraController.camSize`。这在 `camSize` 未超过房间时可能可用，但在 4x、农场/建筑等场景中会让原生 camera range 进入未设计状态。
  - 你看到的“固定场景中心”和“走一段才刷新”很可能不是单纯背景问题，而是 `CameraController` 的 `camSize/range/follow` 语义被 4x 破坏。
  - 当前 API 把全景/room-fit 的思路混进普通游玩 Zoom。小神增强包的全景相机是特殊模式，会 fit room、隐藏玩家/载具并恢复状态；普通 Zoom 不能直接走这种固定场景逻辑。
- 反证/未证实：
  - 尚未实测 `camSize > sceneSize` 的具体数值；但原生代码没有保护该分支，且用户症状与该风险吻合。
  - 尚未排除原生 camera threshold/movingBounds 在超大视野下造成的额外分段移动，但这仍属于 `CameraController` owner 语义未被正确处理。
- 归属：DTMAPI GameBridge / `ICameraZoomApi` runtime；不是 ZoomMod 业务逻辑。
- 需要更新：`docs/api/public-api-matrix.md`、`docs/hook-map/README.md`、`docs/debug/regressions/smoke-matrix.md`、`docs/updates/2026/20260607-0014-camerazoom-api-rebuild.md` 后续应追加 manual QA failure 或降级说明。
- 验收点：4x 下玩家在农场水平/垂直移动 30 秒，相机必须连续跟随人物；不能固定到场景中心，不能靠分段刷新追上。
- blocker 判定：如果无法在不破坏原生 camera range 的情况下保持人物跟随，`ICameraZoomApi` 不能继续承诺可玩大视野，只能降级为实验/失败，或拆出单独 `IPanoramaCameraApi`。

## 问题 2：放大后到一定高度会闪烁

原始反馈：

- 放大后在一定高度，可能涉及背景上下平移，会导致闪烁。
- 功能可以说完全不可用。

审查记录：

- 用户确认事实：4x 状态下，玩家到某些高度会发生闪烁，疑似背景上下平移相关。
- 截图/日志观察：0.4.2 smoke 的 `zoom-4x.png` 是单帧截图，只证明背景在该瞬间铺满；没有验证 `BackgroundRenderer.Update()` 的动态视差、yOrigin、层位置或雾层动画。
- 代码/文档事实：
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2779` 的背景补偿只拿 `DolocAPI.envBackgroundEx`，然后在 `ApplyCameraZoomTransformScale(...)` 中写 `transform.localScale`。
  - `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2812` 只保存/恢复 transform scale；没有处理 position、yOrigin、layer offsets、camera parallax parameters、mask/clipping 或 renderer bounds。
  - 原生 `BackgroundRenderer.Update()` 在 `BackgroundRenderer.cs:85` 每帧用 `yOrigin - cam.transform.position.y` 计算纵向背景层 offset，再逐层调用 `UpdatePosition(...)`。
  - 原生 `ISceneHandle.LoadScene(...)` 在 `ISceneHandle.cs:45` 根据 `sceneInfo.HighLevel_Ref.Offset` 调 `DolocAPI.envBackgroundEx.SetYOrigin(...)`；说明 yOrigin 是场景高度相关 owner，不是单纯 transform scale。
  - 0.4.2 方法体审查曾记录“普通 Zoom 只补强背景/雾层 scale，不进入全景模式”；但实际实现没有验证动态 parallax owner。
- Codex 推断：
  - 背景闪烁大概率来自“全局 transform scale 乘以 4”与原生 `BackgroundRenderer.Update()` 每帧重算 layer position 叠加。原生 renderer 仍按未缩放的 yOrigin 和 camera position 驱动背景层，DTMAPI 只改变父 transform scale，动态高度变化时就可能出现跳变、遮罩错位或层级闪烁。
  - 雾层也只整体 scale，未验证 depth fog renderer 的可见范围、mask、transition tween 与相机位置是否一致。
- 反证/未证实：
  - 尚未定位具体闪烁发生在 `BackgroundRenderer` 还是 `DepthFogController`；需要带日志/视频或逐项关闭 background/fog compensation 才能确定。
  - 尚未证明 camera API 完全不能实现普通大视野；但当前“只改 orthographicSize + CameraController.RefreshResolution + transform scale”的方案已经证明不足。
- 归属：DTMAPI GameBridge background/fog compensation；不是 UI 或 ConfigMenu。
- 需要更新：同问题 1；另需新增/更新 debug 验收，要求移动中截图/录像或连续状态采样，而非静态截图。
- 验收点：4x 下在农场上下移动、上楼/下楼、靠近高低背景边界时持续 30 秒不闪烁，背景/雾层不跳变。
- blocker 判定：如果无法接入 `BackgroundRenderer` 的动态 owner（yOrigin、UpdatePosition、layer offset/mask）或无法绕开它，不能把 CameraZoom 标记为完成。

## 结论

0.4.2 CameraZoom 的当前完成证据不足，且用户手测已证明玩家可见功能失败。失败不是 ZoomMod 没调用 API，而是 `ICameraZoomApi` 的 GameBridge 实现混合了两个不同目标：

- 普通游玩 Zoom：应继续以玩家为中心，缩放视野但保持原生相机跟随语义。
- 全景/拍照/room-fit：可以固定房间中心、fit room、隐藏玩家/载具并恢复状态。

当前实现把普通 Zoom 做成“放大相机 + 强行刷新 room range + 背景父 transform scale”，导致原生 `CameraController` range/follow 与 `BackgroundRenderer` yOrigin/parallax 没有真正被接管。

## 建议方向

1. 立刻把 `ICameraZoomApi` 的 0.4.2 状态从“smoke verified / completion proof”降级为“manual QA failed / experimental broken for live play”。
2. 下一轮不要继续从 ZoomMod 层修；必须先决定 API 目标：
   - 若目标是普通游玩大视野：不能让 `camSize > sceneSize` 破坏 `CameraController` 跟随；需要研究是否只改 `mainCamera.orthographicSize` 并避免 `RefreshResolution()`，或给 `CameraController` 增加安全 range/follow 策略。
   - 若目标是全景/半农场视野：应拆成 `IPanoramaCameraApi`，接受固定房间/隐藏对象/PhotoState/room-fit 语义，不作为普通 Zoom。
3. 验收必须新增动态项：
   - 4x 后保持至少 30 秒；
   - 玩家横向/纵向移动；
   - 经过背景高度变化区域；
   - 进出建筑/返回农场；
   - 验证相机中心、背景、雾层和 UI 同时正常。
