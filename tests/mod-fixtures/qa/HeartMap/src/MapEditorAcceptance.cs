using System;
using System.Collections;
using System.IO;
using System.Linq;
using DTMAPI.MapData;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed partial class MapModule
    {
        private IEnumerator EditorAcceptance()
        {
            var expected = MapCompiler.CompileForPrivateRuntime(File.ReadAllText(Config.AcceptanceExpectedFile));
            if (!expected.Success || Config.EditorFiles.Length != 1) throw new InvalidOperationException("Explicit editor acceptance inputs required");
            var data = expected.Map!; var map = maps.Values.Single(m => m.Content.Definition.Id == data.Id);
            if (!map.Registered || map.Content.Definition.ToJson() != data.ToJson()) throw new InvalidOperationException("Registered input differs from actual browser export");
            Log("QA PASS editor-source map=" + data.Id + "; revision=" + data.Revision + "; sourceSha256=" + MapFileInput.Hash(File.ReadAllBytes(Config.AcceptanceExpectedFile)));
            yield return Exercise("enter", MapResult.Entered);
            yield return new WaitForSecondsRealtime(.8f);
            CheckEditorGeometry(map);
            if (Vector2.Distance(DolocAPI.AgentPosition, map.Spawn) > .3f) throw new InvalidOperationException("Native spawn differs from exported point");
            Log("QA PASS editor-spawn expected=" + map.Spawn + "; actual=" + DolocAPI.AgentPosition);
            ToggleOverview(); Capture("editor-" + map.Content.Room.Width + "x" + map.Content.Room.Height); yield return new WaitForSecondsRealtime(.4f); RestoreOverview();
            yield return Movement("editor");
            var beforeJump = DolocAPI.AgentPosition; float peak = beforeJump.y;
            yield return EditorKey(Key.Space, .15f);
            double jumpUntil = Now + 1.5;
            while (Now < jumpUntil) { peak = Math.Max(peak, DolocAPI.AgentPosition.y); yield return null; }
            if (peak < beforeJump.y + .5f) throw new InvalidOperationException("Native jump did not rise");
            Log("QA PASS editor-native-jump before=" + beforeJump.y + "; peak=" + peak);
            var spawn = map.Content.Spawn.Cell;
            if (map.Content.Room.Platform.Contains((spawn.Y - 1) * map.Content.Room.Width + spawn.X))
            {
                float beforeDrop = DolocAPI.AgentPosition.y;
                yield return EditorKey(Key.S, .25f); yield return new WaitForSecondsRealtime(.8f);
                if (DolocAPI.AgentPosition.y > beforeDrop - 1f) throw new InvalidOperationException("Native one-way drop did not cross exported platform");
                Log("QA PASS editor-platform-drop before=" + beforeDrop + "; after=" + DolocAPI.AgentPosition.y);
                yield return Exercise("farm", MapResult.Exited); yield return Exercise("enter " + data.Id, MapResult.Entered);
                yield return new WaitForSecondsRealtime(.5f);
            }
            double exitUntil = Now + 8;
            while (Math.Abs(DolocAPI.AgentPosition.x - map.Exit.x) > 1f && Now < exitUntil)
                yield return EditorKey(DolocAPI.AgentPosition.x < map.Exit.x ? Key.D : Key.A, .08f);
            if (Vector2.Distance(DolocAPI.AgentPosition, map.Exit) > 2f) throw new InvalidOperationException("Exported exit not reached by native movement");
            Log("QA PASS editor-exit expected=" + map.Exit + "; actual=" + DolocAPI.AgentPosition);
            yield return Exercise("exit", MapResult.Exited);
            if (!string.IsNullOrEmpty(Config.AcceptanceReplacementFile))
            {
                // Copy an actual second browser download, never synthesize geometry.
                File.Copy(Config.AcceptanceReplacementFile, Config.EditorFiles[0], true);
                Log("QA author-file-replaced sha256=" + MapFileInput.Hash(File.ReadAllBytes(Config.EditorFiles[0])));
                yield return Exercise("enter " + data.Id, MapResult.Entered);
                CheckEditorGeometry(map);
                if (map.Content.Definition.ToJson() != data.ToJson()) throw new InvalidOperationException("Active geometry changed before restart");
                Log("QA PASS active-file-change-deferred");
                yield return Exercise("farm", MapResult.Exited);
                DolocAPI.ReturnHome();
                yield return Await(() => !DolocAPI.IsDataLoaded && DolocAPI.userInput.CurrentState?.GetType().Name == "HomePageUiState", 30, "editor title");
                yield return new WaitForSecondsRealtime(.5f); CollectRetired();
                if (ownedScenes.Count != 0) throw new InvalidOperationException("Editor scene retained at title");
                LoadAcceptanceSave(); yield return Await(Ready, 40, "editor reload"); yield return PrepareAcceptanceFarm();
                yield return Exercise("enter " + data.Id, MapResult.Entered); CheckEditorGeometry(map);
                Log("QA PASS title-retains-accepted-input restart-required; revision=" + map.Content.Definition.Revision);
                yield return Exercise("farm", MapResult.Exited);
            }
            foreach (var other in maps.Values.Where(m => m.Content.Definition.Id != data.Id))
            { yield return Exercise("enter " + other.Content.Definition.Id, MapResult.Entered); yield return Exercise("farm", MapResult.Exited); }
            yield return new WaitForSecondsRealtime(.5f); CollectRetired();
            if (ownedScenes.Count != 0) throw new InvalidOperationException("Editor test retained scenes");
            Log("QA PASS editor-all; ownedScenes=0; blockedSaves=" + blockedSaves);
        }
        private IEnumerator EditorKey(Key key, float duration)
        {
            var keyboard = Keyboard.current ?? throw new InvalidOperationException("Native keyboard absent");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(duration);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(.04f);
        }
        private void CheckEditorGeometry(MapRegistration map)
        {
            var r = map.Content.Room; var geometry = DolocAPI.CurrentRoom.Geometry;
            if (geometry.gridSize != new Vector2Int(r.Width, r.Height) || geometry.roomSize != new Vector2(r.Width * 1.5f, r.Height * 1.5f)) throw new InvalidOperationException("Native dimensions differ");
            int[] Indices(Vector2Int[] cells) => cells.Select(c => c.y * r.Width + c.x).OrderBy(i => i).ToArray();
            if (!Indices(geometry.obstacles).SequenceEqual(r.Solid) || !Indices(geometry.platformObstaclePositions).SequenceEqual(r.Platform) || !Indices(geometry.groundPositions).SequenceEqual(r.Ground))
                throw new InvalidOperationException("Native RoomGeometry differs from compiled author cells");
            var layers = map.Scene!.Root.GetComponentsInChildren<Tilemap>();
            var solid = layers.Single(t => t.name == "Heart solid boundary"); var platform = layers.Single(t => t.name == "Heart one-way platforms");
            for (int y = 0; y < r.Height; y++) for (int x = 0; x < r.Width; x++)
                if (solid.HasTile(new Vector3Int(x, y, 0)) != r.IsSolid(x, y) || platform.HasTile(new Vector3Int(x, y, 0)) != r.Platform.Contains(y * r.Width + x))
                    throw new InvalidOperationException("Native Tilemap differs at " + x + "," + y);
            if (map.Scene.ColliderCount < 2 || !map.Scene.Published) throw new InvalidOperationException("Collision publication missing");
            Log("QA PASS editor-native-geometry size=" + r.Width + "x" + r.Height + "; solid=" + r.Solid.Count + "; platform=" + r.Platform.Count + "; all cells matched; colliders=" + map.Scene.ColliderCount);
        }
    }
}
