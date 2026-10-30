using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DolocTown;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace DTMAPI.Doloc25D
{
    internal sealed class VisualGraph : IDisposable
    {
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly ArtLibrary art;
        private readonly Dictionary<int, SpriteItem> sprites = new Dictionary<int, SpriteItem>();
        private readonly Dictionary<int, TileItem> maps = new Dictionary<int, TileItem>();
        private readonly List<SourceHold> hidden = new List<SourceHold>();
        private readonly List<Renderer> extras = new List<Renderer>();
        private readonly List<Material> extraMaterials = new List<Material>();
        private readonly List<Sprite> extraSprites = new List<Sprite>();
        private readonly HashSet<GameObject> spawned = new HashSet<GameObject>();
        private readonly HashSet<Tilemap> changedMaps = new HashSet<Tilemap>();
        private readonly Surface contact;
        private bool disposed;
        private int scans, rebuilds, nativeRestoreFailures;
        internal bool PreviewDirty { get; private set; }
        internal VisualGraph(string assets, Camera camera)
        {
            this.camera = camera; root = new GameObject("Doloc25D.Visuals"); art = new ArtLibrary(assets);
            var shadow = new MeshData(); shadow.Oval(.7f, .55f);
            contact = Surface.FromData(root.transform, "Player.Contact", shadow, art.Material("contact", new Color(.05f, .04f, .08f, .32f)), SortingLayer.NameToID("Default"), -100);
            AddFill("distant-fill", 60, "Sky", -50, new Color(.65f, .67f, .72f, .65f));
            AddFill("near-fill", 32, "Background", -50, new Color(.58f, .60f, .64f, .5f));
        }
        internal void Discover()
        {
            ++scans;
            foreach (var item in sprites.Values.ToArray()) if (!item.Source) { item.Dispose(); sprites.Remove(item.Id); }
            foreach (var item in maps.Values.ToArray()) if (!item.Source) { item.Dispose(); maps.Remove(item.Id); }
            foreach (var r in Object.FindObjectsOfType<SpriteRenderer>())
            {
                if (!Eligible(r) || sprites.ContainsKey(r.GetInstanceID())) continue;
                var item = new SpriteItem(r, root.transform, art, camera);
                sprites.Add(item.Id, item);
            }
            foreach (var r in Object.FindObjectsOfType<TilemapRenderer>())
            {
                if (!Eligible(r)) continue;
                if (!maps.TryGetValue(r.GetInstanceID(), out var item)) { item = new TileItem(r, root.transform, art); maps.Add(item.Id, item); }
                if (item.Refresh()) ++rebuilds;
            }
        }
        private bool Eligible(Renderer r) => r && !r.transform.IsChildOf(root.transform) &&
            r.GetComponentInParent<Canvas>() == null && r.gameObject.layer != LayerMask.NameToLayer("UI");
        internal void Track(GameObject obj) { if (obj) spawned.Add(obj); }
        internal void Changed(Tilemap map)
        {
            if (!map) return;
            changedMaps.Add(map);
            var renderer = map.GetComponent<TilemapRenderer>();
            if (renderer && renderer.sortingLayerName == "SceneUI") PreviewDirty = true;
        }
        internal void Drain()
        {
            if (spawned.Count + changedMaps.Count == 0) return;
            foreach (var item in sprites.Values.ToArray()) if (!item.Source) { item.Dispose(); sprites.Remove(item.Id); }
            foreach (var item in maps.Values.ToArray()) if (!item.Source) { item.Dispose(); maps.Remove(item.Id); }
            foreach (var obj in spawned)
            {
                if (!obj) continue;
                foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>(true))
                    if (Eligible(r) && !sprites.ContainsKey(r.GetInstanceID())) sprites.Add(r.GetInstanceID(), new SpriteItem(r, root.transform, art, camera));
                foreach (var r in obj.GetComponentsInChildren<TilemapRenderer>(true))
                {
                    if (!Eligible(r)) continue;
                    if (!maps.ContainsKey(r.GetInstanceID())) maps.Add(r.GetInstanceID(), new TileItem(r, root.transform, art));
                    changedMaps.Add(r.GetComponent<Tilemap>());
                }
            }
            spawned.Clear();
            foreach (var map in changedMaps)
            {
                if (!map) continue;
                var renderer = map.GetComponent<TilemapRenderer>();
                if (!Eligible(renderer)) continue;
                if (!maps.TryGetValue(renderer.GetInstanceID(), out var item)) { item = new TileItem(renderer, root.transform, art); maps.Add(item.Id, item); }
                if (item.Refresh()) ++rebuilds;
            }
            changedMaps.Clear(); PreviewDirty = false;
        }
        internal void Begin(ViewProjection view, Vector3 nativeCamera, Camera renderedCamera)
        {
            if (DolocAPI.agent != null && DolocAPI.agent.Status != null)
            {
                Vector3 foot = DolocAPI.AgentPosition; foot.z = .58f; foot.y += .025f;
                contact.Renderer.transform.position = foot; contact.Renderer.forceRenderingOff = !DolocAPI.agent.Status.IsGrounded;
            }
            foreach (var item in sprites.Values)
            {
                if (!item.Source) continue;
                bool show = item.Source.enabled && item.Source.gameObject.activeInHierarchy && !item.Source.forceRenderingOff;
                item.Sync(show, view, nativeCamera, renderedCamera);
                if (show) Hide(item.Source);
            }
            foreach (var item in maps.Values)
            {
                if (!item.Source) continue;
                bool show = item.Source.enabled && item.Source.gameObject.activeInHierarchy && !item.Source.forceRenderingOff;
                item.Sync(show, view, nativeCamera);
                if (show) Hide(item.Source);
            }
            foreach (var renderer in extras)
            {
                var sr = (SpriteRenderer)renderer;
                float depth = renderer.name == "distant-fill" ? 60 : 32;
                float distance = (float)(view.Position - new Point3(nativeCamera.x, nativeCamera.y, 0)).Length();
                Vector3 forward = renderedCamera.transform.forward;
                renderer.transform.SetPositionAndRotation(renderedCamera.transform.position + forward * (distance + depth), renderedCamera.transform.rotation);
                float height = 2 * (distance + depth) * (float)view.TanHalf;
                float scale = Math.Max(height / sr.sprite.bounds.size.y, height * renderedCamera.aspect / sr.sprite.bounds.size.x) * 1.6f;
                renderer.transform.localScale = new Vector3(scale, scale, 1);
                renderer.forceRenderingOff = false;
            }
        }
        private void Hide(Renderer r) { hidden.Add(new SourceHold(r, r.forceRenderingOff)); r.forceRenderingOff = true; }
        internal void End()
        {
            foreach (var hold in hidden) if (hold.Renderer) { hold.Renderer.forceRenderingOff = hold.Value; if (hold.Renderer.forceRenderingOff != hold.Value) ++nativeRestoreFailures; }
            hidden.Clear();
            foreach (var item in sprites.Values) item.Hide();
            foreach (var item in maps.Values) item.Hide();
            foreach (var renderer in extras) if (renderer) renderer.forceRenderingOff = true;
            contact.Renderer.forceRenderingOff = true;
        }
        private void AddFill(string id, float depth, string layer, int order, Color tint)
        {
            if (!art.Has(id)) return;
            Texture2D texture = art.Texture(id);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 8);
            extraSprites.Add(sprite);
            var go = new GameObject(id); go.transform.SetParent(root.transform, false);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.color = tint;
            var mat = art.Material(id, Color.white); extraMaterials.Add(mat); r.sharedMaterial = mat;
            r.sortingLayerName = layer; r.sortingOrder = order; r.forceRenderingOff = true; extras.Add(r);
        }
        internal object Diagnostics() => new { Sprites = sprites.Count, Tilemaps = maps.Count,
            Buildings = sprites.Values.Count(s => s.Building != null), TileCount = maps.Values.Sum(m => m.TileCount),
            Meshes = maps.Values.Sum(m => m.MeshCount) + sprites.Values.Sum(s => s.MeshCount),
            Hidden = hidden.Count, Scans = scans, Rebuilds = rebuilds, NativeRestoreFailures = nativeRestoreFailures,
            ArtTextures = art.Count, LiveObjects = root ? root.GetComponentsInChildren<Transform>().Length : 0 };
        internal object Inventory() => new { Buildings = sprites.Values.Where(s => s.Building != null).Select(s => s.Info()).ToArray(),
            Maps = maps.Values.Select(m => m.Info()).ToArray(),
            Sprites = sprites.Values.Select(s => new { s.Source.name, Sprite = s.Source.sprite ? s.Source.sprite.name : "", Path = PathOf(s.Source.transform),
                Position = V(s.Source.transform.position), Layer = s.Source.sortingLayerName }).ToArray() };
        internal static object V(Vector3 p) => new { p.x, p.y, p.z };
        internal static string PathOf(Transform t) { string p = t.name; while (t.parent) { t = t.parent; p = t.name + "/" + p; } return p; }
        public void Dispose()
        {
            if (disposed) return; End();
            foreach (var item in sprites.Values) item.Dispose(); foreach (var item in maps.Values) item.Dispose();
            foreach (var material in extraMaterials) Object.Destroy(material); foreach (var sprite in extraSprites) Object.Destroy(sprite);
            sprites.Clear(); maps.Clear(); extras.Clear(); spawned.Clear(); changedMaps.Clear(); contact.Dispose(); art.Dispose(); Object.Destroy(root); disposed = true;
        }
        private readonly struct SourceHold
        { internal readonly Renderer Renderer; internal readonly bool Value; internal SourceHold(Renderer r, bool value) { Renderer = r; Value = value; } }
    }
    internal static class PointExtensions
    { internal static double Length(this Point3 p) => Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z); }

    internal sealed class ArtLibrary : IDisposable
    {
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Shader shader;
        internal int Count => textures.Count;
        internal ArtLibrary(string path)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ?? Shader.Find("Sprites/Default");
            if (!shader) throw new InvalidOperationException("No supported built-in sprite shader is available.");
            foreach (string id in new[] { "soil-side", "rock-side", "metal-side", "metal-roof", "platform-edge", "grass-edge", "distant-fill", "near-fill" })
            {
                string file = System.IO.Path.Combine(path, id + ".png");
                if (!File.Exists(file)) continue;
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(t, File.ReadAllBytes(file), false)) { Object.Destroy(t); throw new InvalidDataException(file); }
                t.name = "Doloc25D." + id; t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Repeat; textures.Add(id, t);
            }
        }
        internal bool Has(string id) => textures.ContainsKey(id);
        internal Texture2D Texture(string id) => textures.TryGetValue(id, out var t) ? t : Texture2D.whiteTexture;
        internal Material Material(string id, Color color)
        {
            var selected = id == "distant-fill" || id == "near-fill" || id == "contact" ? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") : shader;
            var m = new Material(selected ? selected : shader) { name = "Doloc25D." + id, mainTexture = Texture(id) };
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (m.HasProperty("_RendererColor")) m.SetColor("_RendererColor", Color.white);
            return m;
        }
        public void Dispose() { foreach (var t in textures.Values) Object.Destroy(t); textures.Clear(); }
    }

    internal sealed class SpriteItem : IDisposable
    {
        internal readonly SpriteRenderer Source;
        internal readonly int Id;
        internal Building? Building;
        private readonly GameObject go;
        private readonly SpriteRenderer proxy;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private readonly List<Surface> surfaces = new List<Surface>();
        private readonly bool backdrop;
        private readonly SpriteRenderer? foliage;
        private Sprite? previousSprite;
        private Vector3 previousPosition;
        private Vector3 previousScale;
        private readonly ArtLibrary art;
        internal int MeshCount => surfaces.Count;
        internal SpriteItem(SpriteRenderer source, Transform parent, ArtLibrary art, Camera camera)
        {
            Source = source; Id = source.GetInstanceID(); this.art = art;
            var buildingRenderer = source.GetComponent<BuildingRenderer>();
            Building = buildingRenderer != null ? buildingRenderer.Building : null;
            backdrop = source.transform.IsChildOf(camera.transform);
            go = new GameObject("Sprite." + Id + "." + source.name); go.transform.SetParent(parent, false);
            proxy = go.AddComponent<SpriteRenderer>(); proxy.forceRenderingOff = true;
            if (source.name.StartsWith("game_entity_crop", StringComparison.Ordinal) || (source.sprite && source.sprite.name.StartsWith("sprite_resource_tree", StringComparison.Ordinal)))
            {
                var layer = new GameObject("FoliageDepth"); layer.transform.SetParent(go.transform, false);
                foliage = layer.AddComponent<SpriteRenderer>(); foliage.forceRenderingOff = true;
            }
            UpdateBuilding();
        }
        private void UpdateBuilding()
        {
            var binding = Source.GetComponent<BuildingRenderer>();
            Building = binding != null ? binding.Building : null;
            if (Building == null || !Source.sprite) return;
            if (previousSprite == Source.sprite && previousPosition == Source.transform.position && previousScale == Source.transform.lossyScale) return;
            previousSprite = Source.sprite;
            previousPosition = Source.transform.position; previousScale = Source.transform.lossyScale;
            foreach (var s in surfaces) s.Dispose(); surfaces.Clear();
            var roomPosition = DolocAPI.CurrentRoom.RoomPosition;
            float left = roomPosition.x + Building.Anchor.x * 1.5f;
            float right = left + Building.proto.CoverSize.x * 1.5f;
            // Each native Building has its own anchor, even when facades overlap.
            // Keep the facade's XY exactly; added geometry never shifts its feet.
            float bottom = Building.PositionBottom.y;
            float top = bottom + Math.Max(1, Building.proto.CoverSize.y) * 1.5f;
            float z0 = .04f, z1 = 3f;
            Color sideTint = Building.BuildingName.Contains("container") ? new Color(1.35f, .85f, .52f) :
                Building.BuildingName.Contains("animal") || Building.BuildingName.Contains("barn") ? new Color(1.05f, .46f, .55f) : new Color(.86f, .88f, .84f);
            surfaces.Add(Surface.Quad(go.transform.parent!, "Building.Left", new Vector3(left, bottom, z0), new Vector3(left, top, z0), new Vector3(left, top, z1), new Vector3(left, bottom, z1),
                art.Material("metal-side", new Color(sideTint.r*.82f, sideTint.g*.82f, sideTint.b*.82f, 1)), Source.sortingLayerID, Source.sortingOrder - 2));
            surfaces.Add(Surface.Quad(go.transform.parent!, "Building.Right", new Vector3(right, bottom, z1), new Vector3(right, top, z1), new Vector3(right, top, z0), new Vector3(right, bottom, z0),
                art.Material("metal-side", sideTint), Source.sortingLayerID, Source.sortingOrder - 2));
            surfaces.Add(Surface.Quad(go.transform.parent!, "Building.Roof", new Vector3(left, top, z0), new Vector3(right, top, z0), new Vector3(right, top, z1), new Vector3(left, top, z1),
                art.Material("metal-roof", new Color(.82f, .79f, .75f)), Source.sortingLayerID, Source.sortingOrder - 2));
            surfaces.Add(Surface.Quad(go.transform.parent!, "Building.Bottom", new Vector3(left, bottom, z1), new Vector3(right, bottom, z1), new Vector3(right, bottom, z0), new Vector3(left, bottom, z0),
                art.Material("platform-edge", new Color(.45f, .43f, .47f)), Source.sortingLayerID, Source.sortingOrder - 2));
            surfaces.Add(Surface.Quad(go.transform.parent!, "Building.Back", new Vector3(left, bottom, z1), new Vector3(left, top, z1), new Vector3(right, top, z1), new Vector3(right, bottom, z1),
                art.Material("metal-side", new Color(.53f, .53f, .57f)), Source.sortingLayerID, Source.sortingOrder - 3));
        }
        internal void Sync(bool show, ViewProjection view, Vector3 nativeCamera, Camera camera)
        {
            if (!Source) return;
            UpdateBuilding();
            proxy.sprite = Source.sprite; proxy.sharedMaterial = Source.sharedMaterial; proxy.color = Source.color;
            proxy.flipX = Source.flipX; proxy.flipY = Source.flipY; proxy.drawMode = Source.drawMode; proxy.size = Source.size;
            proxy.sortingLayerID = Source.sortingLayerID; proxy.sortingOrder = Source.sortingOrder;
            Source.GetPropertyBlock(block); proxy.SetPropertyBlock(block);
            Vector3 position = Source.transform.position; Vector3 scale = Source.transform.lossyScale;
            if (backdrop && Source.sprite)
            {
                float distance = (float)(view.Position - new Point3(nativeCamera.x, nativeCamera.y, 0)).Length();
                float depth = Source.sortingLayerName == "Fog" ? -8 : Source.sortingLayerName == "Sky" ? 85 : 45;
                float factor = (distance + depth) / distance;
                Vector3 offset = camera.transform.InverseTransformPoint(position);
                position = camera.transform.position + camera.transform.forward * (distance + depth)
                    + camera.transform.right * offset.x * factor + camera.transform.up * offset.y * factor;
                go.transform.SetPositionAndRotation(position, camera.transform.rotation);
                // Native camera children are backdrops, never terrain. Expand only
                // those cards; interaction geometry does not get XY compensation.
                go.transform.localScale = new Vector3(scale.x * factor * 1.8f, scale.y * factor * 1.8f, scale.z);
            }
            else
            {
                position.z = Source.sortingLayerName == "Background" ? 12 : Source.sortingLayerName == "Sky" ? 45 : Mathf.Clamp(position.z, -100, 100) * .001f;
                go.transform.SetPositionAndRotation(position, Source.transform.rotation); go.transform.localScale = scale;
            }
            proxy.forceRenderingOff = !show;
            if (foliage)
            {
                foliage.sprite = Source.sprite; foliage.sharedMaterial = Source.sharedMaterial;
                foliage.color = Source.color * new Color(.76f, .82f, .72f, 1); foliage.flipX = Source.flipX; foliage.flipY = Source.flipY;
                foliage.sortingLayerID = Source.sortingLayerID; foliage.sortingOrder = Source.sortingOrder - 1;
                foliage.transform.localPosition = new Vector3(0, 0, .55f); foliage.SetPropertyBlock(block); foliage.forceRenderingOff = !show;
            }
            // BuildingRenderer uses alpha as its damage-mask control, not opacity.
            // Preserve that native value on the facade; solid faces track RGB and
            // the renderer's visibility instead of interpreting the mask as alpha.
            foreach (var surface in surfaces) { surface.Renderer.forceRenderingOff = !show; surface.Tint(new Color(Source.color.r, Source.color.g, Source.color.b, 1)); }
        }
        internal void Hide() { if (proxy) proxy.forceRenderingOff = true; if (foliage) foliage.forceRenderingOff = true; foreach (var s in surfaces) s.Renderer.forceRenderingOff = true; }
        internal object Info() => new { EntityId = Building?.id, Uuid = Building?.uuid, RendererId = Id, Name = Building?.BuildingName, Title = Building?.proto.Title,
            Anchor = Building == null ? null : new { Building.Anchor.x, Building.Anchor.y },
            Foot = Building == null ? null : VisualGraph.V(Building.PositionBottom), Sprite = Source.sprite ? Source.sprite.name : "", Depth = 3,
            CoverSize = Building == null ? null : new { Building.proto.CoverSize.x, Building.proto.CoverSize.y },
            NativePosition = VisualGraph.V(Source.transform.position), NativeBoundsMin = VisualGraph.V(Source.bounds.min), NativeBoundsMax = VisualGraph.V(Source.bounds.max) };
        public void Dispose() { foreach (var s in surfaces) s.Dispose(); surfaces.Clear(); Object.Destroy(go); }
    }

    internal sealed class TileItem : IDisposable
    {
        internal readonly TilemapRenderer Source;
        internal readonly int Id;
        private readonly Tilemap map;
        private readonly Transform parent;
        private readonly ArtLibrary art;
        private readonly List<Surface> surfaces = new List<Surface>();
        private Vector3Int[] positions = Array.Empty<Vector3Int>();
        private readonly List<KeyValuePair<Material, Texture>> frontMaterials = new List<KeyValuePair<Material, Texture>>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private TileBase[] tiles = Array.Empty<TileBase>();
        private int signature = int.MinValue;
        private float plane;
        internal int TileCount;
        internal int MeshCount => surfaces.Count;
        internal TileItem(TilemapRenderer source, Transform parent, ArtLibrary art)
        { Source = source; Id = source.GetInstanceID(); map = source.GetComponent<Tilemap>(); this.parent = parent; this.art = art; }
        internal bool Refresh()
        {
            if (!map) return false;
            BoundsInt bounds = map.cellBounds;
            if (bounds.size.x == 0 || bounds.size.y == 0 || bounds.size.z == 0) { if (surfaces.Count == 0) return false; foreach (var s in surfaces) s.Dispose(); surfaces.Clear(); frontMaterials.Clear(); signature = int.MinValue; TileCount = 0; return true; }
            Vector3Int max = bounds.max - Vector3Int.one;
            int count = map.GetTilesRangeCount(bounds.min, max);
            if (count > 200000) throw new InvalidOperationException("Farm tilemap exceeds bounded geometry budget: " + Source.name);
            if (positions.Length < count) { positions = new Vector3Int[count]; tiles = new TileBase[count]; }
            count = map.GetTilesRangeNonAlloc(bounds.min, max, positions, tiles); TileCount = count;
            int hash = count;
            unchecked { for (int i = 0; i < count; ++i) { hash = hash * 31 + positions[i].GetHashCode(); hash = hash * 31 + (tiles[i] ? tiles[i].GetInstanceID() : 0); } }
            if (hash == signature) return false;
            signature = hash; foreach (var s in surfaces) s.Dispose(); surfaces.Clear(); frontMaterials.Clear();
            plane = Source.sortingLayerName == "Background" ? 12 : 0;
            var batches = new Dictionary<Texture, MeshData>(); var cells = new HashSet<Cell>();
            for (int i = 0; i < count; ++i)
            {
                Vector3Int cell = positions[i]; var sprite = map.GetSprite(cell); if (!sprite) continue;
                if (!batches.TryGetValue(sprite.texture, out var mesh)) { mesh = new MeshData(); batches.Add(sprite.texture, mesh); }
                Matrix4x4 m = map.transform.localToWorldMatrix * Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)cell + map.tileAnchor)) * map.orientationMatrix * map.GetTransformMatrix(cell);
                mesh.Sprite(sprite, m, plane, map.GetColor(cell) * map.color); cells.Add(new Cell(cell.x, cell.y));
            }
            foreach (var batch in batches)
            {
                var material = new Material(Source.sharedMaterial) { name = "Doloc25D.TileFront", mainTexture = batch.Key };
                frontMaterials.Add(new KeyValuePair<Material, Texture>(material, batch.Key));
                surfaces.Add(Surface.FromData(parent, "TileFront." + Source.name, batch.Value, material, Source.sortingLayerID, Source.sortingOrder));
            }
            bool ground = Source.sortingLayerName == "Ground" && Source.name.Trim() == "基底";
            bool platform = Source.sortingLayerName == "Platform" && count > 0 && DolocAPI.farmRenderer != null && map == DolocAPI.farmRenderer.RM_platformTilemap;
            if (platform) AddPlatforms();
            if (ground)
            {
                var top = new MeshData(); var side = new MeshData(); var back = new MeshData(); var grass = new MeshData();
                float depth = ground ? 4.5f : 3f;
                foreach (var cell in cells)
                {
                    Vector3 a = map.CellToWorld(new Vector3Int(cell.X, cell.Y, 0));
                    Vector3 b = map.CellToWorld(new Vector3Int(cell.X + 1, cell.Y + 1, 0));
                    a.z = b.z = depth + .03f;
                    back.Quad(a, new Vector3(b.x, a.y, b.z), b, new Vector3(a.x, b.y, a.z), Color.white);
                }
                foreach (var edge in Footprint.Boundary(cells))
                {
                    Vector3 a = map.CellToWorld(new Vector3Int(edge.X0, edge.Y0, 0));
                    Vector3 b = map.CellToWorld(new Vector3Int(edge.X1, edge.Y1, 0)); a.z = b.z = .03f;
                    // The front edge retains the native grid's elevation. Only Z
                    // gains thickness, so player/building feet stay on that edge.
                    var data = edge.Y0 == edge.Y1 && edge.X1 < edge.X0 ? top : side;
                    data.Quad(a, b, b + Vector3.forward * depth, a + Vector3.forward * depth, Color.white);
                    if (ground && ReferenceEquals(data, top))
                    {
                        Vector3 g0 = a + new Vector3(0, -1.5f, depth), g1 = b + new Vector3(0, -1.5f, depth);
                        grass.Quad(g0, g1, g1 + Vector3.up * 3, g0 + Vector3.up * 3, Color.white, true);
                    }
                }
                if (top.Count > 0) surfaces.Add(Surface.FromData(parent, "Top." + Source.name, top, art.Material(platform ? "platform-edge" : "rock-side", new Color(.76f, .71f, .62f)), Source.sortingLayerID, Source.sortingOrder - 2));
                if (side.Count > 0) surfaces.Add(Surface.FromData(parent, "Sides." + Source.name, side, art.Material(platform ? "platform-edge" : "soil-side", new Color(.68f, .61f, .6f)), Source.sortingLayerID, Source.sortingOrder - 2));
                if (back.Count > 0) surfaces.Add(Surface.FromData(parent, "Back." + Source.name, back, art.Material(platform ? "platform-edge" : "soil-side", new Color(.6f, .56f, .53f)), Source.sortingLayerID, Source.sortingOrder - 4));
                if (grass.Count > 0) surfaces.Add(Surface.FromData(parent, "GrassRim", grass, art.Material("grass-edge", new Color(.8f, .85f, .65f)), Source.sortingLayerID, Source.sortingOrder - 1));
            }
            return true;
        }
        private void AddPlatforms()
        {
            var room = DolocAPI.CurrentRoom;
            if (room == null) return;
            var tread = new MeshData(); var frame = new MeshData();
            foreach (var p in room.DM_platform.totalPlatforms)
            {
                var g = p.geometry;
                float left = room.RoomPosition.x + g.Left * 1.5f, right = room.RoomPosition.x + (g.Right + 1) * 1.5f;
                float top = room.RoomPosition.y + (g.Height + 1) * 1.5f;
                if (p.Collider && p.Collider.ptCollider)
                {
                    var points = p.Collider.ptCollider.points;
                    if (points.Length >= 2) { var a = p.Collider.transform.TransformPoint(points[0]); var b = p.Collider.transform.TransformPoint(points[1]); left = a.x; right = b.x; top = a.y; }
                }
                // Native one-way collider is the walking elevation. The tread
                // meets it exactly; added legs use each native support's height.
                tread.Quad(new Vector3(left,top,.03f),new Vector3(right,top,.03f),new Vector3(right,top,3),new Vector3(left,top,3),Color.white);
                frame.Box(left,top-.375f,right,top,.03f,3,false,false);
                for(int side=0;side<2;++side)
                {
                    int height = side == 0 ? g.columnHeight.x : g.columnHeight.y;
                    if(height <= 0) continue;
                    float x = side == 0 ? left+.75f : right-.75f;
                    float bottom = room.RoomPosition.y + (g.Height-height)*1.5f;
                    frame.Box(x-.1875f,bottom,x+.1875f,top-.375f,.03f,.405f,true,true);
                    frame.Box(x-.1875f,bottom,x+.1875f,top-.375f,2.625f,3,true,true);
                }
            }
            if(tread.Count>0) surfaces.Add(Surface.FromData(parent,"Platform.Treads",tread,art.Material("metal-roof",new Color(.76f,.8f,.79f)),Source.sortingLayerID,Source.sortingOrder-2));
            if(frame.Count>0) surfaces.Add(Surface.FromData(parent,"Platform.Frames",frame,art.Material("platform-edge",new Color(.65f,.63f,.63f)),Source.sortingLayerID,Source.sortingOrder-3));
        }
        internal void Sync(bool show, ViewProjection view, Vector3 nativeCamera)
        {
            // World-space construction holograms use SceneUI sorting, but they
            // still belong to the same XY plane as their placement target.
            // The native material changes color without changing its tiles.
            if (Source.sortingLayerName == "SceneUI")
            {
                Source.GetPropertyBlock(block);
                for (int i = 0; i < frontMaterials.Count; ++i)
                {
                    var front = frontMaterials[i];
                    front.Key.CopyPropertiesFromMaterial(Source.sharedMaterial); front.Key.mainTexture = front.Value;
                    surfaces[i].Renderer.SetPropertyBlock(block);
                }
            }
            foreach (var s in surfaces) s.Renderer.forceRenderingOff = !show;
        }
        internal void Hide() { foreach (var s in surfaces) if (s.Renderer) s.Renderer.forceRenderingOff = true; }
        internal object Info() => new { Source.name, Layer = Source.sortingLayerName, TileCount, MeshCount, Plane = plane,
            CellMin = VisualGraph.V(map.cellBounds.min), CellMax = VisualGraph.V(map.cellBounds.max), CellSize = VisualGraph.V(map.cellSize) };
        public void Dispose() { foreach (var s in surfaces) s.Dispose(); surfaces.Clear(); }
    }

    internal sealed class MeshData
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        internal int Count => vertices.Count;
        internal void Sprite(Sprite sprite, Matrix4x4 matrix, float z, Color tint)
        {
            int start = vertices.Count; Vector2[] sv = sprite.vertices, uv = sprite.uv; ushort[] indices = sprite.triangles;
            for (int i = 0; i < sv.Length; ++i) { Vector3 v = matrix.MultiplyPoint3x4(sv[i]); v.z = z; vertices.Add(v); uvs.Add(uv[i]); colors.Add(tint); }
            for (int i = 0; i < indices.Length; ++i) triangles.Add(start + indices[i]);
        }
        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, bool grass = false)
        {
            int i = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            Vector3 normal = Vector3.Cross(b - a, c - a);
            // World-aligned UVs keep adjacent cells continuous and keep metal
            // corrugation vertical on both left and right walls.
            foreach (var v in new[] { a, b, c, d })
                uvs.Add(Mathf.Abs(normal.y) > .001f ? new Vector2(v.x / 6, v.z / 6) :
                    Mathf.Abs(normal.x) > .001f ? new Vector2(v.z / 6, v.y / 6) : new Vector2(v.x / 6, v.y / 6));
            if (grass) { int at = uvs.Count - 4; uvs[at] = new Vector2(a.x / 12, 0); uvs[at+1] = new Vector2(b.x / 12, 0); uvs[at+2] = new Vector2(c.x / 12, 1); uvs[at+3] = new Vector2(d.x / 12, 1); }
            for (int k = 0; k < 4; ++k) colors.Add(color);
            triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i + 2, i + 1, i, i + 3, i + 2, i });
        }
        internal void Oval(float width, float depth)
        {
            vertices.Add(Vector3.zero); uvs.Add(new Vector2(.5f,.5f)); colors.Add(Color.white);
            for (int j=0; j<12; ++j) { double a = j * Math.PI / 6; vertices.Add(new Vector3((float)Math.Cos(a)*width,0,(float)Math.Sin(a)*depth)); uvs.Add(Vector2.zero); colors.Add(Color.white); }
            for (int j=0; j<12; ++j) { int b=j+1,c=(j+1)%12+1; triangles.AddRange(new[]{0,b,c,0,c,b}); }
        }
        internal void Box(float l,float b,float r,float t,float f,float z,bool front,bool top)
        {
            if(front) Quad(new Vector3(l,b,f),new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(l,t,f),Color.white);
            if(top) Quad(new Vector3(l,t,f),new Vector3(r,t,f),new Vector3(r,t,z),new Vector3(l,t,z),Color.white);
            Quad(new Vector3(l,b,z),new Vector3(r,b,z),new Vector3(r,t,z),new Vector3(l,t,z),Color.white);
            Quad(new Vector3(l,b,f),new Vector3(l,t,f),new Vector3(l,t,z),new Vector3(l,b,z),Color.white);
            Quad(new Vector3(r,b,f),new Vector3(r,t,f),new Vector3(r,t,z),new Vector3(r,b,z),Color.white);
            Quad(new Vector3(l,b,f),new Vector3(r,b,f),new Vector3(r,b,z),new Vector3(l,b,z),Color.white);
        }
        internal Mesh Build()
        {
            var mesh = new Mesh { name = "Doloc25D.Geometry", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            // Both triangle windings share vertices; averaging them would cancel
            // normals. The native 2D lighting convention uses a front-facing normal.
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, vertices.Count).ToList()); return mesh;
        }
    }
    internal sealed class Surface : IDisposable
    {
        internal readonly MeshRenderer Renderer;
        private readonly GameObject go;
        private readonly Mesh mesh;
        private readonly Material material;
        private readonly Color baseColor;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private Surface(Transform parent, string name, MeshData data, Material material, int layer, int order)
        {
            go = new GameObject(name); go.transform.SetParent(parent, false); mesh = data.Build(); this.material = material;
            baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; Renderer = go.AddComponent<MeshRenderer>(); Renderer.sharedMaterial = material;
            Renderer.sortingLayerID = layer; Renderer.sortingOrder = order; Renderer.forceRenderingOff = true;
            Renderer.shadowCastingMode = ShadowCastingMode.Off; Renderer.receiveShadows = false;
        }
        internal static Surface FromData(Transform parent, string name, MeshData data, Material mat, int layer, int order) => new Surface(parent, name, data, mat, layer, order);
        internal static Surface Quad(Transform parent, string name, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat, int layer, int order)
        { var data = new MeshData(); data.Quad(a, b, c, d, Color.white); return new Surface(parent, name, data, mat, layer, order); }
        internal void Tint(Color color) { block.SetColor("_Color", baseColor * color); Renderer.SetPropertyBlock(block); }
        public void Dispose() { Object.Destroy(go); Object.Destroy(mesh); Object.Destroy(material); }
    }
}
