using DTMAPI.Abstractions;

namespace OilMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string OilItemId = "dtmapi_oil";
        private const int OilEnergy = 1500;
        private IDtmHelper helper = null!;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            RegisterConfigMenu();
            helper.Monitor.Log(T("mod.loaded", "DTMAPI Oil loaded; official content JSON supplies the oil item."));
            helper.Monitor.Log("OilMod content item=" + OilItemId + " fuelEnergy=" + OilEnergy + " officialJson=item_tbitem.json.");
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, () => { }, () => { });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Oil"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.content", "Official JSON content"));
            menu.AddParagraph(helper.ModManifest, () => string.Format(T("config.item", "Item={0}, fuel={1}."), OilItemId, OilEnergy));
            menu.AddParagraph(helper.ModManifest, () => T("config.note", "The item is defined by official item_tbitem.json; runtime mining/machine drops are handled by DTMAPI APIs."));
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
    }
}
