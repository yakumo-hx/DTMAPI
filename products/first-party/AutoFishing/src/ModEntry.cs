using System;
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper? helper;
        private AutoFishingConfig config = new AutoFishingConfig();
        private NativeFishing? fishing;
        private IInputRegistration? key;
        private bool menuRegistered;

        public override void Entry(IDtmHelper value)
        {
            helper = value;
            config = value.ReadConfig<AutoFishingConfig>();
            config.Normalize();
            fishing = new NativeFishing(config, Monitor);
            fishing.Install();
            key = value.Input.RegisterKeybind("toggle", config.ToggleKey, DtmInputScope.Gameplay);
            value.Events.Input.KeybindPressed += KeyPressed;
            value.Events.GameLoop.UpdateTicked += Tick;
            value.Events.GameLoop.ReturnedToTitle += Returned;
            value.Events.GameLoop.GameLaunched += Launched;
            RegisterMenu();
            Monitor.Log("Independent AutoFishing ready; DTMAPI keybind=" + config.ToggleKey + "; automation starts disabled.");
        }

        private void KeyPressed(object? sender, KeybindPressedEventArgs e)
        {
            if (e.OwnerId == Manifest.UniqueID && e.KeybindId == "toggle") fishing?.Toggle();
        }
        private void Tick(object? sender, UpdateTickedEventArgs e) => fishing?.Tick();
        private void Returned(object? sender, ReturnedToTitleEventArgs e) => fishing?.Stop("returned-to-title");
        private void Launched(object? sender, GameLaunchedEventArgs e) => RegisterMenu();

        private void RegisterMenu()
        {
            if (helper == null || menuRegistered) return;
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null) return;
            menu.Register(Manifest, () =>
            {
                config = new AutoFishingConfig();
                fishing!.Configure(config);
                key!.Update(config.ToggleKey);
            }, () =>
            {
                config.Normalize();
                key!.Update(config.ToggleKey);
                helper.WriteConfig(config);
                fishing!.Configure(config);
            });
            menu.SetDisplayName(Manifest, () => "自动钓鱼");
            menu.AddKeybindOption(Manifest, () => "开启／关闭", () => "面向水面并选中钓竿后使用。原生钓鱼中断时自动关闭。", () => config.ToggleKey, v => config.ToggleKey = v);
            menu.AddBoolOption(Manifest, () => "立刻咬钩", () => "鱼钩入水后省去等待，仍由游戏按钓竿等级抽取鱼。", () => config.InstantBite, v => config.InstantBite = v);
            menu.AddBoolOption(Manifest, () => "跳过小游戏", () => "直接完成有效小游戏，保留钓竿等级限制和无鱼结果。", () => config.SkipMiniGame, v => config.SkipMiniGame = v);
            menu.AddBoolOption(Manifest, () => "钓鱼动画加速", () => "加速摇竿、抛出、鱼钩飞行、入水及收竿；不改变蓄力速度或小游戏速度。", () => config.FastAnimations, v => config.FastAnimations = v);
            menu.AddNumberOption(Manifest, () => "抛竿蓄力幅度", () => "0 为不蓄力，1 为满蓄力。通过实际蓄力控制抛竿距离。", () => config.CastChargeRatio, v => config.CastChargeRatio = (float)v, 0, 1, 0.05);
            menu.AddNumberOption(Manifest, () => "动画倍率", () => "仅在动画加速开启时生效，支持 1～5 倍。", () => config.AnimationMultiplier, v => config.AnimationMultiplier = (float)v, 1, AutoFishingConfig.MaximumAnimationMultiplier, 0.5);
            menuRegistered = true;
        }

        public void Dispose()
        {
            // Core removes event roots after deactivation; helper access is no longer allowed.
            helper = null;
            Cleanup.Run(
                () => { fishing?.Dispose(); fishing = null; },
                () => { key?.Dispose(); key = null; });
        }
    }
}
