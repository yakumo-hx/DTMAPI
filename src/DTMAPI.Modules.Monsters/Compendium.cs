using System;
using System.Linq;
using System.Reflection;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.Asset;
using DolocTown.Config.Monster;
using DolocTown.UI;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // Bypass global sprite lookup. Only our native document holds this asset.
        private sealed class CompendiumSprite : SpriteAsset
        {
            private readonly Sprite sprite;
            internal CompendiumSprite(Sprite sprite) : base(new CfgSpriteAsset("")) { this.sprite = sprite; }
            public override Sprite Asset => sprite;
        }
        private sealed class CompendiumResources
        {
            internal TbMonsterDocument Table = null!;
            internal MonsterDocumentInfo Row = null!;
            internal Texture2D Texture = null!;
            internal Sprite Sprite = null!;
        }
        private static readonly FieldInfo CompendiumContent = NativeAccess.Field(typeof(MonsterDetailData), "<documentContent>k__BackingField");
        private static readonly MethodInfo RefreshBookMonsters = AccessTools.Method(typeof(CollectionBookUiState), "RefreshMonsterList");

        private void InstallCompendiumHooks()
        {
            Patch(AccessTools.Constructor(typeof(MonsterDetailData), new[] { typeof(string) }), postfix: nameof(CompendiumDetailPostfix));
        }
        private MonsterDiagnostic? PrepareCompendium(Binding binding)
        {
            var c = binding.Definition.Definition.Compendium;
            if (c == null) return null;
            string key = binding.Definition.NativeId;
            var table = DolocConfig.Tables.TbMonsterDocument;
            if (table.DataMap.ContainsKey(key) || table.DataList.Any(r => r.Id == key))
                return Error(binding.Definition, "DuplicateId", "compendium", "Native compendium identity is already occupied.");
            var owned = binding.Compendium = new CompendiumResources { Table = table, Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) };
            owned.Texture.name = binding.Definition.Id + ":compendium";
            if (!ImageConversion.LoadImage(owned.Texture, c.Png, false)) return Error(binding.Definition, "PngDecodeFailed", "compendium.png", "Preview PNG decoding failed.");
            owned.Texture.filterMode = FilterMode.Point; owned.Texture.wrapMode = TextureWrapMode.Clamp;
            owned.Sprite = Sprite.Create(owned.Texture, new Rect(0, 0, owned.Texture.width, owned.Texture.height), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            owned.Sprite.name = binding.Definition.Id + ":compendium";
            owned.Texture.Apply(false, true);
            var sprite = new CompendiumSprite(owned.Sprite);
            // Native document nodes grant environmental-research rewards. This display-only
            // slice adds no nodes; native defeat/unread/count records remain authoritative.
            var kind = c.Category == MonsterCompendiumCategory.Machinery ? MonsterType.MACHINERY : c.Category == MonsterCompendiumCategory.Bionics ? MonsterType.BIONICS : c.Category == MonsterCompendiumCategory.Boss ? MonsterType.BOSS : MonsterType.ORGANISM;
            owned.Row = new MonsterDocumentInfo(key, c.Title, kind, sprite, sprite, c.Habitat, Array.Empty<string>(), Array.Empty<DocumentNodeInfo>(), c.ShowLocked, true);
            NativeAccess.SetProperty(owned.Row, "Id_Ref", binding.Row);
            return null;
        }
        private void PublishCompendium(Binding binding)
        {
            var c = binding.Compendium;
            if (c == null) return;
            Fault("Compendium");
            c.Table.DataMap.Add(c.Row.Id, c.Row); c.Table.DataList.Add(c.Row);
        }
        private static void CheckCompendiumOwnership(Binding binding)
        {
            var c = binding.Compendium;
            if (c?.Row == null) return;
            if (c.Table.DataMap.TryGetValue(c.Row.Id, out var row) && !ReferenceEquals(row, c.Row)
                || c.Table.DataList.Any(r => r.Id == c.Row.Id && !ReferenceEquals(r, c.Row)))
                throw new NativeIdentityConflictException("Compendium ownership changed; refusing removal.");
        }
        private void ReleaseCompendium(Binding binding)
        {
            var c = binding.Compendium;
            if (c == null) return;
            CheckCompendiumOwnership(binding);
            if (c.Row != null) { c.Table.DataMap.Remove(c.Row.Id); c.Table.DataList.Remove(c.Row); }
            if (c.Sprite) Object.Destroy(c.Sprite);
            if (c.Texture) Object.Destroy(c.Texture);
            binding.Compendium = null;
        }
        private static void CompendiumDetailPostfix(string monsterId, ref MonsterDetailData __instance)
        {
            var module = current;
            if (module == null || !__instance.isVisited ||
                !module.bindings.TryGetValue(monsterId, out var binding) || binding.Compendium == null) return;
            object boxed = __instance;
            CompendiumContent.SetValue(boxed, binding.Definition.Definition.Compendium!.Description);
            __instance = (MonsterDetailData)boxed;
        }
        private void RefreshOpenCompendium()
        {
            if (!(DolocAPI.userInput?.CurrentState is CollectionBookUiState book)) return;
            RefreshBookMonsters.Invoke(book, null);
            var panel = (MonsterCatalogPanel)AccessTools.Property(typeof(CollectionBookUiState), "monsterPanel").GetValue(book);
            panel.monsterViewer.Render(default);
        }
    }
}
