using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.MonsterContent;
using DolocTown;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        [DataContract] private sealed class FrameCatalog { [DataMember] public SourceIdentity[] sources = Array.Empty<SourceIdentity>(); [DataMember] public FrameTemplate[] templates = Array.Empty<FrameTemplate>(); }
        [DataContract] private sealed class SourceIdentity { [DataMember] public string path = ""; [DataMember] public string sha256 = ""; }
        [DataContract] private sealed class FrameTemplate { [DataMember] public string prefab = ""; [DataMember] public string fingerprint = ""; [DataMember] public FrameInfo[] frames = Array.Empty<FrameInfo>(); }
        [DataContract] private sealed class FrameInfo { [DataMember] public string id = ""; [DataMember] public int width = 0; [DataMember] public int height = 0; [DataMember] public float pivotX = 0; [DataMember] public float pivotY = 0; [DataMember] public float ppu = 0; }
        private sealed class SkinResources
        {
            internal Texture2D Texture = null!;
            internal readonly Dictionary<string, Sprite?> Sprites = new Dictionary<string, Sprite?>(StringComparer.Ordinal);
        }
        private sealed class PresentationState
        {
            internal SpriteRenderer Renderer = null!;
            internal Color OriginalColor;
            internal Sprite? Source, Mapped;
            internal SpriteRenderer? VisualRenderer;
            internal MaterialPropertyBlock? VisualProperties;
            internal bool OriginalForceRenderingOff;
            internal RectTransform? Root, Fill;
            internal GameObject? Bar;
            internal int Health = int.MinValue;
            internal RectTransform? BossRoot, BossFill;
            internal Text? BossText;
            internal int BossHealth = int.MinValue, BossPhase = -1, BossReached = -1;
        }
        private readonly HashSet<InstanceScope> visualScopes = new HashSet<InstanceScope>();
        private readonly Dictionary<SpriteOverrideHandler, InstanceScope> presentationOwners = new Dictionary<SpriteOverrideHandler, InstanceScope>();
        private readonly HashSet<InstanceScope> displayScopes = new HashSet<InstanceScope>();
        private readonly List<InstanceScope> bossScopes = new List<InstanceScope>();
        private readonly Dictionary<string, FrameTemplate> frameTemplates = new Dictionary<string, FrameTemplate>(StringComparer.Ordinal);
        private bool framesLoaded;
        private string framesError = "";
        private GameObject? overlay;
        private Font? overlayFont;
        private MonsterPresentationDriver? overlayDriver;
        private static readonly Func<Monster, int> ReadHealth = HealthAccessor();
        private static Func<Monster, int> HealthAccessor()
        {
            var value = Expression.Parameter(typeof(Monster), "monster");
            return Expression.Lambda<Func<Monster, int>>(Expression.Field(value, NativeAccess.Field(typeof(Monster), "currentHealth")), value).Compile();
        }

        private void LoadFrameCatalog()
        {
            if (framesLoaded) return;
            using var trace = OptimizationTrace.Begin(3);
            framesLoaded = true;
            try
            {
                using var stream = typeof(NativeModule).Assembly.GetManifestResourceStream("DTMAPI.MonsterFrames.json");
                if (stream == null) throw new InvalidDataException("This candidate has no template frame metadata.");
                var catalog = (FrameCatalog)new DataContractJsonSerializer(typeof(FrameCatalog)).ReadObject(stream);
                if (catalog.sources == null || catalog.sources.Length == 0 || catalog.templates == null || catalog.templates.Length == 0) throw new InvalidDataException("Empty or malformed template frame metadata.");
                // Source digests are exporter provenance. Actual prefab/frame mappings are
                // checked when used; hashing unrelated game bundles cannot prove those mappings.
                foreach (var template in catalog.templates) frameTemplates.Add(template.prefab, template);
            }
            catch (Exception ex) { framesError = ex.Message; frameTemplates.Clear(); }
        }
        private MonsterPresentationInfo PresentationInfo(Template template)
        {
            LoadFrameCatalog();
            if (framesError.Length != 0 || template.Prefab == null || !frameTemplates.TryGetValue(template.Prefab.name, out var data))
                return new MonsterPresentationInfo("", Array.Empty<MonsterFrameInfo>(), framesError.Length != 0 ? framesError : "No verified root-renderer frame mapping for prefab '" + template.Prefab?.name + "' (catalog=" + frameTemplates.Count + ").");
            return new MonsterPresentationInfo(data.fingerprint, data.frames.Select(f => new MonsterFrameInfo(f.id, f.width, f.height, f.pivotX, f.pivotY, f.ppu)));
        }
        private MonsterDiagnostic? PreparePresentation(Binding binding)
        {
            using var trace = OptimizationTrace.Begin(12);
            var skin = binding.Definition.Definition.Appearance.Skin;
            if (skin == null) return null;
            var info = PresentationInfo(binding.Template);
            MonsterDiagnostic ErrorAt(string code, string field, string message) => Error(binding.Definition, code, "appearance.skin." + field, message);
            var validation = DTMAPI.MonsterContent.Hosting.SkinMappingAdmission.Validate(binding.Definition.Owner, binding.Definition.Definition, info);
            if (validation != null) return validation;
            if (binding.Template.Prefab!.GetComponent<SpriteOverrideHandler>() == null) return ErrorAt("TemplateFramesUnavailable", "templateFingerprint", "Template has no root sprite override boundary.");
            binding.Skin = CreateSkin(skin, info, binding.Definition.Id);
            return null;
        }
        private static SkinResources CreateSkin(MonsterSkin skin, MonsterPresentationInfo info, string id)
        {
            var owned = new SkinResources { Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) };
            try
            {
                owned.Texture.name = id + ":skin";
                if (!ImageConversion.LoadImage(owned.Texture, skin.Png, false)) throw new InvalidDataException("PNG decoding failed.");
                owned.Texture.filterMode = FilterMode.Point; owned.Texture.wrapMode = TextureWrapMode.Clamp;
                var mapping = skin.Frames.ToDictionary(f => f.FrameId, StringComparer.Ordinal);
                foreach (var frame in info.Frames)
                {
                    mapping.TryGetValue(frame.Id, out var r);
                    if (!DTMAPI.MonsterContent.Hosting.SkinMappingAdmission.HasPixels(r, owned.Texture.width, owned.Texture.height))
                    {
                        owned.Sprites.Add(frame.Id, null);
                        continue;
                    }
                    var sprite = Sprite.Create(owned.Texture, new Rect(r!.X, owned.Texture.height - r.Y - r.Height, r.Width, r.Height), new Vector2(frame.PivotX, frame.PivotY), frame.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                    sprite.name = id + ":" + frame.Id;
                    owned.Sprites.Add(frame.Id, sprite);
                }
                owned.Texture.Apply(false, true);
                return owned;
            }
            catch { ReleaseSkin(owned); throw; }
        }
        private static void ReleaseSkin(SkinResources skin)
        {
            foreach (var sprite in skin.Sprites.Values) if (sprite != null) Object.Destroy(sprite);
            Object.Destroy(skin.Texture);
        }
        private static void ReleasePresentation(Binding binding)
        {
            ReleaseAnimations(binding);
            if (binding.Skin == null) return;
            ReleaseSkin(binding.Skin); binding.Skin = null;
        }
        private void InstallPresentationHooks() => Patch(AccessTools.Method(typeof(SpriteOverrideHandler), "LateUpdate"), prefix: nameof(PresentationSpritePrefix), postfix: nameof(PresentationSpritePostfix));
        private static bool PresentationSpritePrefix(SpriteOverrideHandler __instance)
        {
            var module = current;
            if (module == null) return true;
            if (!module.presentationOwners.TryGetValue(__instance, out var scope)) return module.MapNativeSkin(__instance);
            if (scope.Action?.Animation != null) return false;
            if (scope.Binding.Skin == null) return true;
            if (!scope.Active) return false;
            var state = scope.Presentation!;
            var source = state.Renderer.sprite;
            if (source == null || source == state.Mapped) return false;
            scope.Binding.Skin.Sprites.TryGetValue(source.name, out var sprite);
            state.Source = source; state.Mapped = sprite; state.Renderer.sprite = sprite;
            return false; // Only this instance's explicitly mapped body bypasses global overrides.
        }
        private static float VisualScale(Binding binding) => binding.NativeChild ? binding.Definition.Definition.NativeParts.FirstOrDefault(p => p.TemplateId == binding.Template.Proto.Name)?.Scale ?? 1 : binding.Definition.Definition.Appearance.Scale ?? 1;
        private static void PresentationSpritePostfix(SpriteOverrideHandler __instance)
        {
            if (current == null || !current.presentationOwners.TryGetValue(__instance, out var scope) || !scope.Active) return;
            ApplyVisualScale(scope);
        }
        private static void ApplyVisualScale(InstanceScope scope)
        {
            if (!scope.Active || scope.Presentation?.VisualRenderer == null) return;
            var state = scope.Presentation; var source = state.Renderer; var visual = state.VisualRenderer;
            // Mirror the native renderer after its animation/skin/effect updates. Only
            // this renderer-only child carries visual scale; physics and attack anchors
            // stay on the untouched native hierarchy. No source Sprite is modified.
            visual.sprite = source.sprite; visual.color = source.color;
            visual.flipX = source.flipX; visual.flipY = source.flipY;
            visual.sharedMaterial = source.sharedMaterial;
            visual.sortingLayerID = source.sortingLayerID; visual.sortingOrder = source.sortingOrder;
            visual.maskInteraction = source.maskInteraction; visual.spriteSortPoint = source.spriteSortPoint;
            visual.drawMode = source.drawMode; visual.size = source.size;
            visual.enabled = source.enabled && !state.OriginalForceRenderingOff;
            source.GetPropertyBlock(state.VisualProperties!); visual.SetPropertyBlock(state.VisualProperties!);
            source.forceRenderingOff = true;
        }
        private void BindPresentation(InstanceScope scope)
        {
            if (scope.Binding.NativeChild && VisualScale(scope.Binding) == 1) return;
            var controller = scope.Controller!;
            var state = scope.Presentation = new PresentationState { Renderer = controller.Renderer.SpriteRenderer };
            state.OriginalColor = state.Renderer.color;
            ColorUtility.TryParseHtmlString(scope.Binding.NativeChild ? "#FFFFFF" : scope.Binding.Definition.Definition.Appearance.Tint, out var tint);
            state.Renderer.color = state.OriginalColor * tint;
            var handler = controller.GetComponent<SpriteOverrideHandler>();
            if (scope.Binding.Skin != null && handler == null) throw new InvalidOperationException("Template has no root sprite override boundary.");
            if (handler != null) presentationOwners[handler] = scope;
            if (VisualScale(scope.Binding) != 1)
            {
                var view = new GameObject("DTMAPI visual size"); view.layer = state.Renderer.gameObject.layer;
                view.transform.SetParent(state.Renderer.transform, false);
                view.transform.localScale = Vector3.one * VisualScale(scope.Binding);
                state.VisualRenderer = view.AddComponent<SpriteRenderer>();
                state.VisualProperties = new MaterialPropertyBlock();
                state.OriginalForceRenderingOff = state.Renderer.forceRenderingOff;
                visualScopes.Add(scope); ApplyVisualScale(scope); EnsureOverlay(); overlayDriver!.enabled = true;
            }
            if (scope.Binding.NativeChild) return;
            var definition = scope.Binding.Definition.Definition;
            var display = definition.Display;
            if (display.BossBar)
            {
                EnsureOverlay();
                if (overlayFont == null) overlayFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 14);
                state.BossRoot = NewRect(scope.Binding.Definition.Id + ":Boss", overlay!.transform, new Vector2(460, 42));
                state.BossRoot.gameObject.SetActive(false);
                state.BossRoot.anchorMin = state.BossRoot.anchorMax = new Vector2(.5f, 1);
                var textRoot = NewRect("Label", state.BossRoot, new Vector2(460, 24)); textRoot.anchoredPosition = new Vector2(0, 9);
                var text = state.BossText = textRoot.gameObject.AddComponent<Text>();
                text.font = overlayFont; text.fontSize = 14; text.alignment = TextAnchor.MiddleCenter; text.supportRichText = false; text.raycastTarget = false;
                textRoot.gameObject.AddComponent<Outline>().effectColor = Color.black;
                var bar = NewRect("Health", state.BossRoot, new Vector2(460, 10)); bar.anchoredPosition = new Vector2(0, -10);
                var background = bar.gameObject.AddComponent<Image>(); background.color = new Color(.05f, .05f, .05f, .9f); background.raycastTarget = false;
                state.BossFill = NewRect("Fill", bar, Vector2.zero); state.BossFill.anchorMin = Vector2.zero; state.BossFill.anchorMax = Vector2.one;
                state.BossFill.offsetMin = new Vector2(1, 1); state.BossFill.offsetMax = new Vector2(-1, -1);
                var fill = state.BossFill.gameObject.AddComponent<Image>(); fill.color = new Color(.85f, .25f, .2f); fill.raycastTarget = false;
                bossScopes.Add(scope); displayScopes.Add(scope); overlayDriver!.enabled = true;
            }
            if (!display.Nameplate && display.HealthBar == MonsterHealthBar.Never) return;
            EnsureOverlay();
            state.Root = NewRect(scope.Binding.Definition.Id, overlay!.transform, new Vector2(120, 30));
            state.Root.gameObject.SetActive(false);
            if (display.Nameplate)
            {
                if (overlayFont == null) overlayFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 14);
                var labelRoot = NewRect("Name", state.Root, new Vector2(120, 20)); labelRoot.anchoredPosition = new Vector2(0, 8);
                var label = labelRoot.gameObject.AddComponent<Text>(); label.font = overlayFont; label.fontSize = 14; label.alignment = TextAnchor.MiddleCenter;
                label.supportRichText = false; label.raycastTarget = false; label.color = Color.white;
                label.text = string.IsNullOrWhiteSpace(display.Name) ? definition.LocalId : display.Name;
                var outline = labelRoot.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black;
            }
            if (display.HealthBar != MonsterHealthBar.Never)
            {
                var bar = NewRect("Health", state.Root, new Vector2(48, 5)); bar.anchoredPosition = new Vector2(0, -7);
                var background = bar.gameObject.AddComponent<Image>(); background.color = new Color(.05f, .05f, .05f, .85f); background.raycastTarget = false;
                state.Bar = bar.gameObject;
                state.Fill = NewRect("Fill", bar, Vector2.zero); state.Fill.anchorMin = Vector2.zero; state.Fill.anchorMax = Vector2.one; state.Fill.offsetMin = new Vector2(1, 1); state.Fill.offsetMax = new Vector2(-1, -1);
                var fill = state.Fill.gameObject.AddComponent<Image>(); fill.color = new Color(.45f, .85f, .3f); fill.raycastTarget = false;
            }
            displayScopes.Add(scope);
            overlayDriver!.enabled = true;
        }
        private static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false); rect.sizeDelta = size; return rect;
        }
        private void EnsureOverlay()
        {
            if (overlay != null) return;
            overlay = new GameObject("DTMAPI Monster UI", typeof(RectTransform), typeof(Canvas));
            var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            // At most one canvas/driver and (only for names) one font per module.
            // Keep them for reuse; Dispose releases them after the final owner exits.
            overlayDriver = overlay.AddComponent<MonsterPresentationDriver>();
            overlayDriver.enabled = false;
            overlayDriver.UpdatePresentation = UpdatePresentation;
        }
        private void UpdatePresentation()
        {
             {
            foreach (var scope in visualScopes) ApplyVisualScale(scope);
            var camera = Camera.main;
            int bossRow = 0;
            foreach (var scope in bossScopes)
            {
                var state = scope.Presentation!;
                bool visible = scope.Active && scope.Controller != null && scope.Controller.gameObject.activeInHierarchy &&
                    scope.Logical != null && !scope.Logical.IsDead && ReferenceEquals(scope.Host, DolocAPI.CurrentRoom);
                int hp = visible ? ReadHealth(scope.Logical!) : 0;
                visible &= hp > 0;
                if (state.BossRoot!.gameObject.activeSelf != visible) state.BossRoot.gameObject.SetActive(visible);
                if (!visible) continue;
                state.BossRoot.anchoredPosition = new Vector2(0, -38 - 48 * bossRow++);
                int phase = scope.HealthPhase?.Index ?? 0, reached = scope.HealthPhase?.ReachedIndex ?? 0;
                if (state.BossHealth == hp && state.BossPhase == phase && state.BossReached == reached) continue;
                state.BossHealth = hp; state.BossPhase = phase; state.BossReached = reached;
                int maximum = scope.Logical!.proto.Health;
                state.BossFill!.anchorMax = new Vector2(Mathf.Clamp01((float)hp / maximum), 1);
                var definition = scope.Binding.Definition.Definition;
                string name = string.IsNullOrWhiteSpace(definition.Display.Name) ? definition.LocalId : definition.Display.Name;
                string stage = phase == 0 ? "基础阶段" : definition.HealthPhases!.Stages[phase - 1].Id;
                string pending = reached > phase ? " → " + definition.HealthPhases!.Stages[reached - 1].Id : "";
                state.BossText!.text = name + "   " + hp + " / " + maximum + (scope.HealthPhase == null ? "" : "   " + stage + pending);
            }
            foreach (var scope in displayScopes)
            {
                var state = scope.Presentation;
                if (state?.Root == null) continue;
                bool active = camera != null && scope.Active && scope.Controller != null && scope.Controller.gameObject.activeInHierarchy && state.Renderer.enabled && scope.Logical != null && !scope.Logical.IsDead;
                var point = active ? camera!.WorldToScreenPoint(state.Renderer.bounds.max + new Vector3(-state.Renderer.bounds.extents.x, .15f, 0)) : Vector3.zero;
                bool visible = active && point.z > 0 && point.x >= 0 && point.x <= Screen.width && point.y >= 0 && point.y <= Screen.height;
                if (state.Root.gameObject.activeSelf != visible) state.Root.gameObject.SetActive(visible);
                if (!visible) continue;
                state.Root.position = new Vector3(point.x, point.y, 0);
                if (state.Bar == null) continue;
                int hp = ReadHealth(scope.Logical!);
                var display = scope.Binding.Definition.Definition.Display;
                bool show = display.HealthBar == MonsterHealthBar.Always || display.HealthBar == MonsterHealthBar.Damaged && hp < scope.Logical!.proto.Health;
                if (state.Bar.activeSelf != show) state.Bar.SetActive(show);
                if (hp != state.Health) { state.Health = hp; state.Fill!.anchorMax = new Vector2(Mathf.Clamp01((float)hp / scope.Logical!.proto.Health), 1); }
            }
            }
        }
        private void UnbindPresentation(InstanceScope scope)
        {
            visualScopes.Remove(scope);
            displayScopes.Remove(scope);
            bossScopes.Remove(scope);
            if (displayScopes.Count == 0 && visualScopes.Count == 0 && overlayDriver != null) overlayDriver.enabled = false;
            var state = scope.Presentation;
            if (state == null) return;
            if (state.Renderer != null)
            {
                state.Renderer.color = state.OriginalColor;
                if (state.VisualRenderer != null) state.Renderer.forceRenderingOff = state.OriginalForceRenderingOff;
                    if (state.Source != null) state.Renderer.sprite = state.Source;
                var handler = state.Renderer.GetComponent<SpriteOverrideHandler>();
                if (handler != null) presentationOwners.Remove(handler);
            }
            if (state.VisualRenderer != null) Object.Destroy(state.VisualRenderer.gameObject);
            if (state.Root != null) Object.Destroy(state.Root.gameObject);
            if (state.BossRoot != null) Object.Destroy(state.BossRoot.gameObject);
            scope.Presentation = null;
        }
    }
    [DefaultExecutionOrder(32000)]
    public sealed class MonsterPresentationDriver : MonoBehaviour
    {
        internal Action? UpdatePresentation;
        private void LateUpdate() => UpdatePresentation?.Invoke();
        private void OnDestroy() { UpdatePresentation = null; }
    }
}
