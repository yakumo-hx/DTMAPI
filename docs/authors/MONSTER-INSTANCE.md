# 两位独立巡卫：普通 CodeMod 工程

使用 Runtime 0.7.5 内置怪物模块和配套纯契约。套件 examples/InstanceCode 含普通 netstandard2.0 csproj、ModEntry.cs、package 目录及 contracts/DTMAPI.MonsterContent.dll 编译引用。它只依赖公开 Abstractions 和 MonsterContent 契约；SDK 可选。

## 行为如何运行

两只巡卫继承本机原生 amoeba_radiation 模板，使用原创名称和颜色，不分发官方图片或实现。它们通过正常房间出生规则进入多洛可河谷的 stage2_巨大的树，各占一个Ground槽；有空槽且位置可达才成立，不清除原版怪物腾位置。

RegisterBehavior 的工厂每次执行 new Patrol。每个对象在 Start 保存自己的实例句柄、起点、计时和动作句柄；Tick只使用自己的游戏时间和GetAction结果。橙色短班巡卫等待一个基础间隔，向一侧巡游再返回，然后通过自身Owner的Despawn结束；青色留守巡卫每次等三个基础间隔并继续往返。一个对象结束时只移除自己的状态，不重置另一实例。

移动使用当前 Runtime已有的MoveTo，最大动作时间10游戏秒、最大路径长度12。出发侧不可达时只试一次另一侧；动作失败/超时后保留blocked状态和具体日志，不每帧重试或直接改坐标。Stop放弃上下文与句柄，由原生模块清理原生动作；已关闭的上下文不能再Cancel。离房结束这次可见执行，重新可见时由原生模块创建新的行为对象与新状态，不承诺跨房间保存巡逻计时。

命令 Samples.MonsterInstanceCode/patrol 只读当前状态与实例位置。日志中的instance、phase、clock、wait、legs和action分别说明当前对象；它们不是整个Mod共享的计时器。模块/规则Ready与实际实例和实际移动应分别核对。

## 构建、安装与再创作

使用 PowerShell 5.1+ 和 .NET 8 SDK，在套件根运行：

脚本名保留既有拼写，内部为标准工程构建和目录打包。SDK prepare 和生成证明都不是前提。复制工程到套件外时，可用 `-p:DtmApiReferences=<套件/contracts>` 指定 API 编译引用。保留源码与 references，编译引用不随产品复制。

退出游戏，安装 Runtime，再把作品目录放入官方 MODS 并启用；也可使用可选 SDK deploy。进入多洛可河谷的 stage2_巨大的树，确认两个实例实际出现，橙色先动、青色等待，各自移动。没有原生空位时需换房间或减少数量，登记成功不能代替生成成功。离房与停用清理动作和实例；更换已驻留 DLL 需要冷启动。

修改 PauseGameSeconds（初值 1.5），提高 package/info.json 的版本，构建后退出游戏再部署。SDK `deploy` 默认覆盖同名文件，删除或改名后的旧文件仍会留下；需清除残留时用 `--clean`，它不备份旧安装。`--replace` 只是冗余覆盖确认。`withdraw/recover` 保留手动撤回和恢复能力。普通诊断可按需运行；契约与运行依赖见[内容 Host](CONTENT-HOSTS.md)。
