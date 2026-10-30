using System;
using System.IO;
using DTMAPI.Abstractions;

namespace DTMAPI.Doloc25D
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        internal static ModEntry? Current;
        private IDtmHelper helper = null!;
        internal Doloc25DConfig Config = new Doloc25DConfig();
        internal FarmPresentation Runtime = null!;
        private IInputRegistration? toggle, preset;
        private int presetIndex;
        private bool disposed;
        public override void Entry(IDtmHelper value)
        {
            helper = value; Current = this;
            Config = helper.ReadConfig<Doloc25DConfig>() ?? new Doloc25DConfig(); Config.Normalize();
            string assets = Path.Combine(Path.GetDirectoryName(typeof(ModEntry).Assembly.Location)!, "art");
            Runtime = new FarmPresentation(helper, Config, assets);
            try
            {
                Runtime.Install();
                Rebind();
                helper.Events.Input.KeybindPressed += Key;
                helper.Events.GameLoop.UpdateTicked += Tick;
                helper.Events.Save.SaveLoaded += Loaded;
                helper.Events.GameLoop.ReturnedToTitle += Title;
                Menu();
                helper.Monitor.Log("农场立体化 V0.1：F8 原版/立体，F9 镜头预设；仅平地农场。", LogLevel.Info);
            }
            catch { Dispose(); throw; }
        }
        private void Tick(object sender, UpdateTickedEventArgs e) => Runtime.Tick();
        private void Loaded(object sender, SaveLoadedEventArgs e) => Runtime.SceneChanged();
        private void Title(object sender, ReturnedToTitleEventArgs e) => Runtime.SceneChanged();
        private void Key(object sender, KeybindPressedEventArgs e)
        {
            if (e.OwnerId != helper.ModManifest.UniqueID || !Runtime.SupportedRoom || !(DolocAPI.userInput?.CurrentState is DolocTown.NormalGameState)) return;
            if (e.KeybindId == "view.toggle") { Config.Enabled = !Config.Enabled; Runtime.ClearFault(); }
            else if (e.KeybindId == "view.preset") { Config.Preset(++presetIndex); Config.Enabled = true; Runtime.ClearFault(); }
            else return;
            helper.WriteConfig(Config);
        }
        private void Rebind()
        {
            toggle?.Dispose(); preset?.Dispose();
            toggle = helper.Input.RegisterKeybind("view.toggle", Config.ToggleKey, DtmInputScope.Gameplay);
            preset = helper.Input.RegisterKeybind("view.preset", Config.PresetKey, DtmInputScope.Gameplay);
        }
        private void Menu()
        {
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null) return;
            menu.Register(helper.ModManifest, () => { Config = new Doloc25DConfig(); Runtime.Configure(Config); },
                () => { Config.Normalize(); Runtime.Configure(Config); Rebind(); helper.WriteConfig(Config); });
            menu.AddSectionTitle(helper.ModManifest, () => "农场立体化 V0.1 · F8 开关 / F9 预设");
            menu.AddBoolOption(helper.ModManifest, () => "启用立体农场", () => "离开平地农场后自动恢复原版。", () => Config.Enabled, v => Config.Enabled = v);
            menu.AddNumberOption(helper.ModManifest, () => "水平角度", () => "−35° 至 35°", () => Config.Yaw, v => Config.Yaw = v, -35, 35, 1);
            menu.AddNumberOption(helper.ModManifest, () => "俯视角度", () => "0° 至 15°", () => Config.Pitch, v => Config.Pitch = v, 0, 15, 1);
            menu.AddNumberOption(helper.ModManifest, () => "视野倍率", () => "0.85 较近，1.20 较远", () => Config.ViewScale, v => Config.ViewScale = v, .85, 1.20, .05);
            menu.AddKeybindOption(helper.ModManifest, () => "原版/立体切换", () => "仅游戏操作时响应。", () => Config.ToggleKey, v => Config.ToggleKey = v);
            menu.AddKeybindOption(helper.ModManifest, () => "切换镜头预设", () => "默认、正面、左斜、右斜、近景。", () => Config.PresetKey, v => Config.PresetKey = v);
        }
        public void Dispose()
        {
            if (disposed) return;
            Runtime?.Dispose(); toggle?.Dispose(); preset?.Dispose();
            // The owner removes its helper event roots during normal disposal.
            Current = null; disposed = true;
        }
    }
}
