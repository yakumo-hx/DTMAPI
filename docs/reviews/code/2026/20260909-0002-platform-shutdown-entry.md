# PN-020 原生退出入口缺口

- Date: `2026-09-09`
- Status: `verified`
- Owning Update: [PN-009](../../../updates/2026/20260909-0004-platform-context-scheduler.md)。

## 观察与边界

PN-009 的 QA Application.Quit 和 PN-020 准备启动的 Alt+F4 都正常结束进程，但没有 Bootstrap OnApplicationQuit、Core ShuttingDown 或普通 owner 关闭记录。已知 Bootstrap 的 Unity Update 同样不被调度，现有 PlayerLoop/InputSystem 驱动承担更新；仅有进程退出不能证明清理执行。准备启动尚未启用外部 Mod，不能以该次记录推断其 Dispose。

本机 UnityEngine.CoreModule 中 `UnityEngine.Application.quitting` 是静态 Action 事件，native 调用 `Internal_ApplicationQuit()` 发布该事件；`wantsToQuit` 是退出请求/否决阶段，不适合作为提交后的关闭边界。只核对这组方法和元数据，未复制 Unity 实现。

## 决定

Bootstrap 在 Awake 完成 Runtime 准备后，通过反射订阅 `Application.quitting`，复用同一个幂等关闭方法；保留 MonoBehaviour OnApplicationQuit 入口。开始关闭时解除静态事件订阅，然后释放帧驱动、UI、Bridge 与 Core。该适配不新增 Harmony owner、线程、计时器或 public API，也不拦截/取消原生退出。每个独立关闭阶段隔离异常，避免一个 UI 释放失败跳过 Core owner 清理。

## 验收

原生冷启动/正常退出须记录退出事件、ShuttingDown 主线程快照、外部 Mod Dispose 和资源 LIFO 释放；同一关闭调用只能执行一次。SDK/ABI 字节不因 Bootstrap 修复重写。原有失败记录保留，普通退出与 QA 退出分别验证。

## 实测收口

[PN-020](../../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md) 的 E 正常退出按钮与 G 新游戏/IO 重试后的 QA Application.Quit 均通过：Application.quitting 一次、thread 1、ShuttingDown、两 Mod Dispose、LIFO、全部 remaining=0/failures=0；三个最终 runner Passed。UnityEngine.CoreModule 身份留在 unity-core-module.json。未改旧失败日志，不新增生命周期 owner。
