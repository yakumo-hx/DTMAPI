using System;
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.ActionSpeed;

public sealed class ModEntry : DtmMod, IDisposable
{
    private IDtmHelper? helper;
    private ActionConfig config = new ActionConfig();
    private NativeActions? native;

    public override void Entry(IDtmHelper helper)
    {
        this.helper = helper;
        config = ConfigMigration.Load(helper);
        native = new NativeActions(() => config);
        try
        {
            native.Install();
            RegisterPage();
            helper.Events.Save.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += OnTitle;
            Monitor.Log($"动作加速 {Manifest.Version} 已加载，设置入口在 Mod 配置中。", LogLevel.Info);
        }
        catch { Dispose(); throw; }
    }

    private void RegisterPage()
    {
        var menu = helper!.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu")
            ?? throw new InvalidOperationException("DTMAPI.ModConfigMenu is required.");
        menu.Register(Manifest, () => config = new ActionConfig(), () =>
        {
            native?.Reset();
            helper.WriteConfig(config);
        });
        menu.SetDisplayName(Manifest, () => "DTMAPI动作加速");
        Add("工具加速", "斧、镐、镰刀和水壶手动浇水。", () => config.ToolEnabled, v => config.ToolEnabled = v, () => config.ToolMultiplier, v => config.ToolMultiplier = v);
        Add("装水加速", "空瓶装水、水壶补水；水中和合法水容器均适用。", () => config.FillEnabled, v => config.FillEnabled = v, () => config.FillMultiplier, v => config.FillMultiplier = v);
        Add("吃喝加速", "加快吃喝动画；保留普通食物的原生连续使用节奏。", () => config.EatEnabled, v => config.EatEnabled = v, () => config.EatMultiplier, v => config.EatMultiplier = v);
        Add("交互加速", "加燃料、加饲料、抚摸动物、取奶及电器交互。", () => config.InteractEnabled, v => config.InteractEnabled = v, () => config.InteractMultiplier, v => config.InteractMultiplier = v);
        Add("采集加速", "收获作物、可采集物和树脂。", () => config.GatherEnabled, v => config.GatherEnabled = v, () => config.GatherMultiplier, v => config.GatherMultiplier = v);
        Add("种植加速", "播种、种树、施肥和作物保护膜，包括种植枪的动作。", () => config.PlantEnabled, v => config.PlantEnabled = v, () => config.PlantMultiplier, v => config.PlantMultiplier = v);
        menu.AddInlineBoolBoolOption(Manifest, () => "自动装水", () => "在水中选中空瓶时自动装水；仍需原生动作资格和体力。动画速度由装水加速单独控制。",
            () => config.AutoFill, v => config.AutoFill = v, () => "强力", () => $"尝试间隔从 {config.AutoFillInterval:0.##} 秒缩短至 {config.StrongAutoFillInterval:0.##} 秒；不会打断正在进行的动作。",
            () => config.StrongAutoFill, v => config.StrongAutoFill = v);
        menu.AddBoolOption(Manifest, () => "右键持续喝水", () => "按住右键饮用瓶装水时，原生分段计时以 3 倍推进。吃喝动画由吃喝加速单独控制。",
            () => config.ContinuousDrink, v => config.ContinuousDrink = v);
        void Add(string name, string tip, Func<bool> enabled, Action<bool> setEnabled, Func<double> value, Action<double> setValue) =>
            menu.AddInlineBoolNumberOption(Manifest, () => name, () => tip, enabled, setEnabled, value, v => setValue(ActionConfig.Limit(v)), 1, 4, 0.1);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => native?.Reset();
    private void OnTitle(object? sender, ReturnedToTitleEventArgs e) => native?.Reset();

    public void Dispose()
    {
        // Runtime owns event subscriptions and may close the owner before disposal.
        helper = null;
        native?.Dispose();
        native = null;
    }
}
