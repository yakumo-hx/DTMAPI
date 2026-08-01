using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace ConfigMenuExample
{
    public sealed class ModEntry : DtmMod
    {
        private ExampleConfig config = new ExampleConfig();

        public override void Entry(IDtmHelper helper)
        {
            config = helper.ReadConfig<ExampleConfig>();
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log("Config menu API not available.", LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, () =>
            {
                config = new ExampleConfig();
            }, () => helper.WriteConfig(config), titleScreenOnly: false);

            menu.SetDisplayName(helper.ModManifest, () => "配置菜单示例（开发者）");
            menu.AddSectionTitle(helper.ModManifest, () => "示例选项（开发者）");
            menu.AddBoolOption(helper.ModManifest, () => "启用", () => "示例布尔选项。", () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => "速度", () => "示例数字选项。", () => config.Speed, value => config.Speed = value, 0, 10, 0.5);
            menu.AddTextOption(helper.ModManifest, () => "标签", () => "示例文本选项。", () => config.Label, value => config.Label = value);
            menu.AddChoiceOption(helper.ModManifest, () => "模式", () => "示例选项列表。", () => config.Mode, value => config.Mode = value, new[] { "Safe", "Fast", "Debug" });
            menu.AddKeybindOption(helper.ModManifest, () => "切换按键", () => "示例按键绑定。", () => config.ToggleKey, value => config.ToggleKey = value);
            helper.Monitor.Log("ConfigMenuExample registered config page.");
        }

        [DataContract]
        public sealed class ExampleConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public double Speed { get; set; } = 1.0;
            [DataMember] public string Label { get; set; } = "DTMAPI";
            [DataMember] public string Mode { get; set; } = "Safe";
            [DataMember] public string ToggleKey { get; set; } = "F9";
        }
    }
}
