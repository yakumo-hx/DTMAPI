using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private readonly Dictionary<string, MonsterDocumentInfo> missingDocuments = new Dictionary<string, MonsterDocumentInfo>(StringComparer.Ordinal);
        private TbMonsterDocument? missingDocumentTable;
        private Texture2D? missingPreviewTexture;
        private Sprite? missingPreview;
        private bool compendiumStopping;
        private static readonly FieldInfo CompendiumContent = NativeAccess.Field(typeof(MonsterDetailData), "<documentContent>k__BackingField");
        private static readonly MethodInfo RefreshBookMonsters = AccessTools.Method(typeof(CollectionBookUiState), "RefreshMonsterList");

        private void InstallCompendiumHooks()
        {
            Patch(RefreshBookMonsters, prefix: nameof(CompendiumListPrefix));
            Patch(AccessTools.Method(typeof(CollectionBookUiState), "Hide"), postfix: nameof(CompendiumHidden));
            Patch(AccessTools.Constructor(typeof(MonsterDetailData), new[] { typeof(string) }), postfix: nameof(CompendiumDetailPostfix));
        }
        private MonsterDiagnostic? PrepareCompendium(Binding binding)
        {
            var c = binding.Definition.Definition.Compendium;
            if (c == null) return null;
            string key = binding.Definition.NativeId;
            RemoveMissingDocument(key);
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
            if (module == null || !__instance.isVisited) return;
            string? description = null;
            if (module.bindings.TryGetValue(monsterId, out var binding) && binding.Compendium != null)
                description = binding.Definition.Definition.Compendium!.Description;
            else if (module.missingDocuments.ContainsKey(monsterId) && TryReadContentId(monsterId, out var id))
                description = "此内容当前不可用。重新启用对应内容包后可恢复图文；原有击杀和已读记录仍保留。\n\n内容 ID：" + id;
            if (description == null) return;
            object boxed = __instance;
            CompendiumContent.SetValue(boxed, description);
            __instance = (MonsterDetailData)boxed;
        }
        private static void CompendiumListPrefix() => current?.RefreshMissingDocuments();
        private static void CompendiumHidden() => current?.ClearMissingDocuments();
        private void RefreshMissingDocuments()
        {
            ClearMissingDocuments();
            if (compendiumStopping || !Ready || DolocAPI.archiveHandle?.farmData?.collectionManager == null) return;
            var table = missingDocumentTable = DolocConfig.Tables.TbMonsterDocument;
            foreach (var pair in DolocAPI.archiveHandle.farmData.collectionManager.monsterCollections)
            {
                if (!pair.Value.isUnlock || bindings.ContainsKey(pair.Key) || table.DataMap.ContainsKey(pair.Key) || table.DataList.Any(r => r.Id == pair.Key) || !TryReadContentId(pair.Key, out var id)) continue;
                var sprite = new CompendiumSprite(MissingPreview());
                var row = new MonsterDocumentInfo(pair.Key, "内容不可用", MonsterType.ORGANISM, sprite, sprite, "对应内容包未启用",
                    Array.Empty<string>(), Array.Empty<DocumentNodeInfo>(), false, true);
                table.DataMap.Add(pair.Key, row); table.DataList.Add(row); missingDocuments.Add(pair.Key, row);
            }
        }
        private static bool TryReadContentId(string key, out string id)
        {
            id = "";
            if (!key.StartsWith("dtm_mc_", StringComparison.Ordinal) || key.Length > 2064) return false;
            var parts = key.Substring(7).Split('_');
            if (parts.Length != 2 || parts.Any(p => p.Length == 0 || p.Length % 2 != 0 || p.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))) return false;
            try
            {
                string Decode(string p) => new UTF8Encoding(false, true).GetString(Enumerable.Range(0, p.Length / 2).Select(i => Convert.ToByte(p.Substring(i * 2, 2), 16)).ToArray());
                var owner = Decode(parts[0]); var local = Decode(parts[1]);
                if (!MonsterAdmission.ValidLocalId(owner) || !MonsterAdmission.ValidLocalId(local)) return false;
                id = owner + ":" + local; return true;
            }
            catch (Exception e) when (e is FormatException || e is DecoderFallbackException) { return false; }
        }
        private Sprite MissingPreview()
        {
            if (missingPreview) return missingPreview!;
            missingPreviewTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "DTMAPI.Monsters:missing", filterMode = FilterMode.Point };
            var pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) pixels[y * 32 + x] = x == y || x + y == 31 ? new Color(.9f, .65f, .3f) : new Color(.15f, .2f, .25f);
            missingPreviewTexture.SetPixels(pixels); missingPreviewTexture.Apply(false, true);
            missingPreview = Sprite.Create(missingPreviewTexture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 100);
            return missingPreview;
        }
        private void RemoveMissingDocument(string key)
        {
            if (!missingDocuments.TryGetValue(key, out var row)) return;
            if (missingDocumentTable != null)
            {
                if (missingDocumentTable.DataMap.TryGetValue(key, out var currentRow) && ReferenceEquals(currentRow, row)) missingDocumentTable.DataMap.Remove(key);
                missingDocumentTable.DataList.Remove(row);
            }
            missingDocuments.Remove(key);
        }
        private void ClearMissingDocuments()
        {
            foreach (var key in missingDocuments.Keys.ToArray()) RemoveMissingDocument(key);
            missingDocumentTable = null;
        }
        private void RefreshOpenCompendium()
        {
            if (!(DolocAPI.userInput?.CurrentState is CollectionBookUiState book)) return;
            RefreshBookMonsters.Invoke(book, null);
            var panel = (MonsterCatalogPanel)AccessTools.Property(typeof(CollectionBookUiState), "monsterPanel").GetValue(book);
            panel.monsterViewer.Render(default);
        }
        private void DisposeCompendium()
        {
            compendiumStopping = true;
            ClearMissingDocuments(); RefreshOpenCompendium();
            if (missingPreview) Object.Destroy(missingPreview);
            if (missingPreviewTexture) Object.Destroy(missingPreviewTexture);
            missingPreview = null; missingPreviewTexture = null;
        }
    }
}
