using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DolocTown;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using Scenes = UnityEngine.SceneManagement.SceneManager;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed class MapScene
    {
        internal readonly MapContent Content;
        internal readonly int Generation;
        internal readonly long RequestId;
        internal bool Retired { get; private set; }
        internal AsyncOperation? Unloading { get; private set; }
        internal bool UnloadFailed { get; private set; }
        internal Vector2 Spawn => new Vector2((float)Content.Spawn.WorldX, (float)Content.Spawn.WorldY);
        internal MapScene(MapContent content, int generation, long requestId)
        { Content = content; Generation = generation; RequestId = requestId; }
        // This is a private version adapter, resolved once before any scene is built.
        private static readonly FieldInfo GroundField = RequireTilemap("tilemap");
        private static readonly FieldInfo PlatformField = RequireTilemap("platformTilemap");
        private static readonly FieldInfo DecorationField = RequireTilemap("decoratorTilemap");
        private readonly List<Object> assets = new List<Object>();
        internal Scene Scene { get; private set; }
        internal RoomHandle Handle { get; private set; } = null!;
        internal GameObject Root { get; private set; } = null!;
        internal int ColliderCount { get; private set; }
        internal int TileCount { get; private set; }
        internal bool Published { get; private set; }
        internal double MaxBuildSliceMs { get; private set; }
        internal string MaxBuildSliceStage { get; private set; } = string.Empty;
        internal double CollisionCommitMs { get; private set; }
        private TilemapCollider2D? groundCollider;
        internal IEnumerable<Object> OwnedAssets => assets;
        internal ISceneHandle[] Handles { get; private set; } = Array.Empty<ISceneHandle>();

        private static FieldInfo RequireTilemap(string name)
        {
            MapMetrics.ReflectionResolutions++;
            var field = typeof(RoomHandle).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(Tilemap)) throw new MissingFieldException(typeof(RoomHandle).FullName, name);
            return field;
        }

        internal IEnumerator Build(bool injectFailure)
        {
            var data = Content.Room;
            var platformCells = new HashSet<int>(data.Platform);
            var slice = Stopwatch.StartNew();
            Scene = Scenes.CreateScene(Content.SceneName);
            Root = new GameObject("DTMAPI Heart Map (internal NoNativeSave)");
            Root.SetActive(false);
            Scenes.MoveGameObjectToScene(Root, Scene);
            var grid = Root.AddComponent<Grid>();
            grid.cellSize = new Vector3(MapContent.CellSize, MapContent.CellSize, 1);
            EndSlice("scene-grid"); yield return null; slice.Restart();
            Tile groundTile = Tile("heart_stone", new Color32(69, 39, 72, 255), new Color32(179, 87, 131, 255));
            EndSlice("ground-texture"); yield return null; slice.Restart();
            Tile edgeTile = Tile("heart_edge", new Color32(175, 73, 112, 255), new Color32(255, 178, 171, 255));
            EndSlice("edge-texture"); yield return null; slice.Restart();
            Tile backgroundTile = Tile("heart_background", new Color32(38, 63, 74, 255), new Color32(47, 76, 86, 255));
            EndSlice("background-texture"); yield return null; slice.Restart();
            Tile platformTile = Tile("heart_platform", new Color32(90, 152, 157, 255), new Color32(195, 238, 210, 255));
            EndSlice("platform-texture"); yield return null; slice.Restart();
            Tilemap ground = Layer("Heart solid boundary", "Ground", "Ground");
            Tilemap background = Layer("Heart open background", "Default", "BuildingsBackground");
            Tilemap platform = Layer("Heart one-way platforms", "Platform", "Platform");
            Tilemap decoration = Layer("Heart decoration", "Default", "BuildingsForeground");
            var positions = new Vector3Int[data.Width * data.Height];
            var solids = new TileBase[positions.Length];
            var backs = new TileBase[positions.Length];
            var platforms = new TileBase[positions.Length];
            int index = 0;
            for (int y = 0; y < data.Height; y++)
            for (int x = 0; x < data.Width; x++, index++)
            {
                positions[index] = new Vector3Int(x, y, 0);
                if (data.IsSolid(x, y))
                {
                    bool edge = IsOpen(x - 1, y) || IsOpen(x + 1, y) || IsOpen(x, y - 1) || IsOpen(x, y + 1);
                    solids[index] = edge ? edgeTile : groundTile;
                }
                else backs[index] = backgroundTile;
                if (platformCells.Contains(y * data.Width + x)) platforms[index] = platformTile;
            }
            ground.SetTiles(positions, solids);
            EndSlice("tilemaps-layout-ground"); yield return null; slice.Restart();
            background.SetTiles(positions, backs);
            EndSlice("background-tiles"); yield return null; slice.Restart();
            platform.SetTiles(positions, platforms);
            TileCount = positions.Length + data.Platform.Count;
            if (injectFailure) throw new InvalidOperationException("MAP-INJECTED: failure after owned tile allocation, before scene publication.");
            EndSlice("platform-tiles"); yield return null; slice.Restart();

            var rigidbody = ground.gameObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            var tilesCollider = ground.gameObject.AddComponent<TilemapCollider2D>();
            groundCollider = tilesCollider;
            tilesCollider.usedByComposite = true;
            var composite = ground.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            ColliderCount = 2;
            // Native one-way floors are top edges on the Platform physics layer.
            for (int y = 0; y < data.Height; y++)
            for (int x = 0; x < data.Width; x++)
            {
                if (!platformCells.Contains(y * data.Width + x)) continue;
                int left = x;
                while (x + 1 < data.Width && platformCells.Contains(y * data.Width + x + 1)) x++;
                // GroundChecker recognizes the native marker, not the physics layer alone.
                // One object per contiguous ledge keeps its enter/exit membership distinct.
                var ledge = new GameObject("Heart ledge " + y);
                ledge.transform.SetParent(platform.transform, false);
                ledge.layer = platform.gameObject.layer;
                var marker = ledge.AddComponent<PlatformEntityOneway>();
                marker.Init();
                var effector = ledge.GetComponent<PlatformEffector2D>();
                effector.useColliderMask = false; // Honor native Player/PlatformExclude collision matrix.
                effector.useOneWay = true;
                effector.useOneWayGrouping = true;
                effector.useSideFriction = false;
                var edge = ledge.GetComponent<EdgeCollider2D>();
                float top = (y + 1) * MapContent.CellSize;
                edge.points = new[] { new Vector2(left * MapContent.CellSize, top), new Vector2((x + 1) * MapContent.CellSize, top) };
                edge.usedByEffector = true;
                ColliderCount++;
            }
            EndSlice("colliders"); yield return null; slice.Restart();

            var roomObject = new GameObject(Content.RoomName);
            roomObject.transform.SetParent(Root.transform, false);
            Handle = roomObject.AddComponent<RoomHandle>();
            Handles = new ISceneHandle[] { Handle };
            Handle.autoGenData = false;
            Handle.roomType = RoomType.Dungeon;
            Handle.isIndoorRoom = false;
            Handle.scenePos = Handle.roomPosition = Vector2.zero;
            Handle.sceneSize = Handle.roomSize = new Vector2(data.Width, data.Height) * MapContent.CellSize;
            Handle.defaultEntryPosition = Spawn;
            Handle.interactableObjects = Array.Empty<InteractableObject>();
            Handle.disableGizmos = true;
            Handle.airWallCollider = roomObject.AddComponent<EdgeCollider2D>();
            Handle.airWallCollider.enabled = false; // Solid outer grid owns the complete boundary.
            GroundField.SetValue(Handle, ground);
            PlatformField.SetValue(Handle, platform);
            DecorationField.SetValue(Handle, decoration);
            ColliderCount++;
            var dungeon = Root.AddComponent<DungeonHandle>();
            dungeon.autoGenerateData = false;
            dungeon.entranceRoom = Handle;
            dungeon.subRoomContainer = Root.transform;
            dungeon.baseMap = ground;
            dungeon.ptMap = platform;
            dungeon.dctMap = decoration;
            EndSlice("room-dungeon"); yield return null; slice.Restart();

            AddLabel("出口  F9 → 主农场", new Vector2((float)Content.Exit.WorldX, (float)Content.Exit.WorldY + 3f), .45f);
            EndSlice("font-exit-label");
            // Keep the complete scene invisible until the native fade/load boundary.
            void EndSlice(string stage)
            {
                double elapsed = slice.Elapsed.TotalMilliseconds;
                if (elapsed <= MaxBuildSliceMs) return;
                MaxBuildSliceMs = elapsed;
                MaxBuildSliceStage = stage;
            }
            bool IsOpen(int x, int y) => x >= 0 && y >= 0 && x < data.Width && y < data.Height && !data.IsSolid(x, y);
        }

        internal void Publish(bool injectFailure = false)
        {
            if (Published) { SetVisible(true); return; }
            var watch = Stopwatch.StartNew();
            Root.SetActive(true);
            if (injectFailure) throw new InvalidOperationException("MAP-INJECTED: publication failed after root activation, before collision commit.");
            groundCollider!.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            CollisionCommitMs = watch.Elapsed.TotalMilliseconds;
            Published = true;
        }
        internal void SetVisible(bool visible) { if (!Retired && Root != null) Root.SetActive(visible); }

        private Tilemap Layer(string name, string physicsLayer, string sortingLayer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root.transform, false);
            go.layer = LayerMask.NameToLayer(physicsLayer);
            if (go.layer < 0) throw new InvalidOperationException("Required physics layer missing: " + physicsLayer);
            var map = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            return map;
        }

        private Tile Tile(string name, Color32 fill, Color32 trim)
        {
            var texture = new Texture2D(12, 12, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            assets.Add(texture);
            var pixels = new Color32[144];
            for (int y = 0; y < 12; y++)
            for (int x = 0; x < 12; x++) pixels[y * 12 + x] = y == 11 || (y == 10 && x % 4 == 0) ? trim : fill;
            texture.SetPixels32(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 12, 12), new Vector2(.5f, .5f), 8);
            sprite.name = name; assets.Add(sprite);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name; tile.sprite = sprite; tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
            assets.Add(tile);
            return tile;
        }

        private Font? labelFont;
        private void AddLabel(string text, Vector2 position, float size)
        {
            if (labelFont == null) { labelFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 32); assets.Add(labelFont); }
            var go = new GameObject("Map sign");
            go.transform.SetParent(Root.transform, false);
            go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = labelFont; mesh.text = text; mesh.fontSize = 32; mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.color = new Color32(255, 218, 195, 255);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = labelFont.material;
            renderer.sortingLayerName = "BuildingsForeground";
        }

        internal string DescribeResources()
        {
            long decodedTileTextureBytes = 0, reportedNativeBytes = 0;
            int measured = 0, total = 0;
            foreach (Object asset in assets)
            {
                if (asset is Texture2D texture) decodedTileTextureBytes += (long)texture.width * texture.height * 4;
                AddSize(asset);
            }
            var components = Root.GetComponentsInChildren<Component>(true);
            foreach (var component in components) AddSize(component);
            long fontAtlasBytes = labelFont?.material?.mainTexture is Texture atlas ? (long)atlas.width * atlas.height * 4 : 0;
            return "resources ownScenes=1; ownAssets=" + assets.Count + "; components=" + components.Length +
                "; ownColliders=" + ColliderCount + "; tileCells=" + TileCount + "; decodedTileTextureBytes=" + decodedTileTextureBytes +
                "; fontAtlasRgbaEstimateBytes=" + fontAtlasBytes + "; measuredNativeObjects=" + measured + "/" + total +
                "; partialNativeObjectBytes=" + reportedNativeBytes + "; zero profiler results mean unavailable; atlas estimate excludes renderer/collider overhead";
            void AddSize(Object value)
            {
                total++;
                long bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(value);
                if (bytes > 0) { measured++; reportedNativeBytes += bytes; }
            }
        }

        internal AsyncOperation? Unload(bool injectFailure = false)
        {
            if (Retired) return Unloading;
            Retired = true;
            // Do not destroy borrowed native fonts/materials/backgrounds. Everything here is ours.
            if (Root != null) { Root.SetActive(false); Object.Destroy(Root); }
            foreach (Object asset in assets) if (asset != null) Object.Destroy(asset);
            assets.Clear(); labelFont = null; Handle = null!; Root = null!;
            groundCollider = null; Published = false;
            Handles = Array.Empty<ISceneHandle>();
            try
            {
                if (injectFailure) throw new InvalidOperationException("MAP-INJECTED: unload refused after owned root disabled.");
                Unloading = Scene.IsValid() && Scene.isLoaded ? Scenes.UnloadSceneAsync(Scene) : null;
                UnloadFailed = Scene.IsValid() && Scene.isLoaded && Unloading == null;
                return Unloading;
            }
            catch { UnloadFailed = true; throw; }
        }
        private bool retriedAtTitle;
        internal void RetryUnloadAtTitle()
        {
            if (!Retired || !UnloadFailed || retriedAtTitle) return;
            retriedAtTitle = true;
            Unloading = Scene.IsValid() && Scene.isLoaded ? Scenes.UnloadSceneAsync(Scene) : null;
            UnloadFailed = Scene.IsValid() && Scene.isLoaded && Unloading == null;
        }
    }
}
