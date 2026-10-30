using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace DTMAPI.ZoomSceneProbe
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private static ModEntry? current;
        private IDtmHelper helper = null!;
        private Settings settings = null!;
        private object? zoom;
        private Type? zoomType;
        private readonly Harmony guard = new Harmony("DTMAPI.ZoomSceneProbe.Native");
        private int ticks, loads, saves, sequence;
        private double lastScale = -1;
        private float deadline;
        private bool oldBackground;
        private int observerFailures;
        private int movementUntil;
        private object? configRegistry;
        private InputField? focusField;
        private DolocTown.InputNameUiState? focusState;
        private DolocTown.UI.InputNameBox? focusPanel;
        private Keyboard? textKeyboard;
        private string imeComposition = "";
        private int endEditEvents, closeStartFrame, closePhase;
        private bool focusClosed, quitRequested, quitIssued, closeNeedsEndEdit;
        private float closeDeadline;
        private string closeReason = "";
        private IDtmInputDiagnostics? inputDiagnostics;
        private readonly Dictionary<string, object> controls = new Dictionary<string, object>();
        private readonly Dictionary<string, bool> previousKeys = new Dictionary<string, bool>();
        private readonly List<ObservedSprite> observed = new List<ObservedSprite>();
        private static readonly string[] controlNames = { "escapeKey", "aKey", "dKey", "equalsKey", "minusKey", "numpadPlusKey", "numpadMinusKey", "leftShiftKey", "rightShiftKey", "f7Key", "f8Key" };
        public override void Entry(IDtmHelper value)
        {
            helper = value; current = this; settings = helper.ReadConfig<Settings>();
            inputDiagnostics = helper.GetOptionalService<IDtmInputDiagnostics>();
            if (!Path.IsPathRooted(settings.Evidence) || DateTime.Parse(settings.ExpiresUtc).ToUniversalTime() < DateTime.UtcNow) throw new Exception("Fresh evidence settings required");
            if (!Path.IsPathRooted(settings.ZoomConfigPath) || Path.GetFileName(settings.ZoomConfigPath) != "DTMAPI.ZoomMod.json") throw new Exception("Runner-bound Zoom config evidence path required");
            Directory.CreateDirectory(settings.Evidence);
            deadline = Time.realtimeSinceStartup + 900;
            oldBackground = Application.runInBackground; Application.runInBackground = true;
            var prefix = new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(RejectSave), BindingFlags.Static | BindingFlags.Public));
            guard.Patch(typeof(DolocAPI).GetMethod("SaveGame", new[] { typeof(int) }), prefix: prefix);
            guard.Patch(typeof(DolocTown.GameData.DataPersistenceManager).GetMethod("SaveGame", new[] { typeof(int) }), prefix: prefix);
            // Advanced Owner supervision freezes the complete patch set at Entry.
            AttachProductObservation();
            helper.Events.GameLoop.UpdateTicked += Tick;
            helper.Events.Save.SaveLoaded += Loaded;
            helper.Events.Save.SaveSaved += Saved;
            helper.Events.GameLoop.ReturnedToTitle += Title;
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu") ?? throw new Exception("Real ConfigMenu required");
            configRegistry = menu.GetType().GetField("inner", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(menu) ?? menu;
            Write("entry.json", new { Owner = helper.ModManifest.UniqueID, NativeHashRequired = "F5E3CB", Expires = settings.ExpiresUtc });
        }
        public static bool RejectSave(ref bool __result)
        { __result = false; current?.Write("save-blocked.json", new { NativeSaveAttempt = true }); return false; }
        private void Loaded(object sender, SaveLoadedEventArgs e) { ++loads; Snapshot("save-loaded"); }
        private void Saved(object sender, SaveSavedEventArgs e) { ++saves; Write("unexpected-save.json", new { saves }); }
        private void Title(object sender, ReturnedToTitleEventArgs e) { Snapshot("returned-title"); }
        private void Tick(object sender, UpdateTickedEventArgs e)
        {
            ++ticks;
            try
            {
                if (Time.realtimeSinceStartup > deadline && !quitRequested) { Write("timeout.json", new { ticks }); RequestQuit("observer deadline"); }
                ObserveInput(); TickCore();
            }
            catch (Exception ex)
            {
                Write("observer-failure-" + ticks + ".json", new { Error = ex.ToString(), Tick = ticks });
                if (++observerFailures == 3 && !quitRequested) RequestQuit("observer failures");
            }
        }
        private void ObserveInput()
        {
            if (textKeyboard == null && Keyboard.current != null)
            {
                textKeyboard = Keyboard.current;
                textKeyboard.onTextInput += ObserveTextCharacter;
                textKeyboard.onIMECompositionChange += ObserveImeComposition;
            }
            if (controls.Count == 0)
            {
                var keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem") ?? throw new Exception("New Input System Keyboard missing");
                var keyboard = keyboardType.GetProperty("current")!.GetValue(null);
                if (keyboard == null) return;
                foreach (string name in controlNames) controls.Add(name, keyboardType.GetProperty(name)!.GetValue(keyboard));
                Write("input-system.json", new { Type = keyboardType.AssemblyQualifiedName, Mvid = keyboardType.Assembly.ManifestModule.ModuleVersionId, Controls = controlNames });
            }
            foreach (var key in controls)
            {
                bool held = (bool)key.Value.GetType().GetProperty("isPressed")!.GetValue(key.Value);
                bool down = (bool)key.Value.GetType().GetProperty("wasPressedThisFrame")!.GetValue(key.Value);
                bool up = (bool)key.Value.GetType().GetProperty("wasReleasedThisFrame")!.GetValue(key.Value);
                bool previous = previousKeys.TryGetValue(key.Key, out bool old) && old;
                if (held != previous || down || up) Append("native-input.jsonl", new { Key = key.Key, Held = held, Down = down, Up = up, Tick = ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Scale = Read("CurrentViewScale"), ApplicationFocused = Application.isFocused, Text = TextFocus(), NativeNormal = DolocAPI.IsNormalState });
                previousKeys[key.Key] = held;
            }
        }
        private void TickCore()
        {
            AdvanceFocusClose();
            if (quitRequested && (focusState == null || focusClosed) && !quitIssued)
            { quitIssued = true; Snapshot("before-quit"); Application.Quit(); return; }
            if (ticks == 1)
                Write("assemblies.json", AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name!.StartsWith("DTMAPI.") || a.GetName().Name == "Assembly-CSharp").Select(a => new { Name = a.FullName, Path = a.Location, Mvid = a.ManifestModule.ModuleVersionId }).ToArray());
            if (ticks < movementUntil && ticks % 5 == 0)
            {
                Append("movement.jsonl", new { ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Scale = Read("CurrentViewScale"), Player = V(DolocAPI.AgentPosition), Camera = DolocAPI.mainCamera ? V(DolocAPI.mainCamera.transform.position) : null, NativeMove = DolocAPI.UserInput.BaseMove.ToString("F4"), Sprites = observed.Where(o => o.Renderer).Select(o => new { o.Id, o.Name, Position = V(o.Renderer.transform.position), Scale = V(o.Renderer.transform.localScale), Offset = o.Renderer.sharedMaterial && o.Renderer.sharedMaterial.HasProperty("_Offset") ? o.Renderer.sharedMaterial.GetVector("_Offset").ToString("F4") : "n/a" }).ToArray() });
                if (ticks % 60 == 0) Snapshot("walking-" + ticks);
            }
            if (ticks % 15 != 0) return;
            Write("status.json", new { Tick = ticks, Utc = DateTime.UtcNow, Loads = loads, NativeNormal = DolocAPI.IsNormalState, NativeState = DolocAPI.userInput?.CurrentState?.GetType().FullName, Room = DolocAPI.CurrentRoom?.RoomId, Scale = Read("CurrentViewScale") });
            double scale = Convert.ToDouble(Read("CurrentViewScale"));
            if (lastScale != scale) { lastScale = scale; Snapshot("scale-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            string file = Path.Combine(settings.Evidence, "command.txt");
            if (!File.Exists(file)) return;
            string command = File.ReadAllText(file).Trim(); File.Delete(file);
            if (command.StartsWith("snap:")) Snapshot(command.Substring(5));
            else if (command == "outdoor")
            {
                var house = DolocAPI.CurrentRoom as DolocTown.TemplateRoomInHouse;
                if (house == null || house.Building == null || house.RootRoom == house) throw new Exception("Current scene is not a house with an exact outdoor destination.");
                Write("programmatic-outdoor.json", new { From = house.RoomId, To = house.RootRoom.RoomId, Position = house.Building.EntryPosition.ToString("F4"), PhysicalInput = false });
                DolocAPI.EnterRoom(house.RootRoom, house.Building.EntryPosition);
            }
            else if (command == "walk-observe") movementUntil = ticks + 360;
            else if (command.StartsWith("rebind:"))
            {
                var page = ConfigPage("DTMAPI.ZoomMod");
                page.GetType().GetMethod("BeginEditing")!.Invoke(page, null);
                var item = page.Items.Single(i => i.ItemId == "DTMAPI.ZoomMod:Keybind:4");
                if (!item.TrySetPendingValue(command.Substring(7), out string error)) throw new Exception(error);
                Write("config-pending-" + ticks + ".json", new { ProgrammaticRegisteredConfigTransaction = true, item.ItemId, item.PendingValue, page.HasPendingChanges });
                page.GetType().GetMethod("Save")!.Invoke(page, null);
                Write("config-saved-" + ticks + ".json", new { ProgrammaticRegisteredConfigTransaction = "ConfigMenuPage.BeginEditing / IConfigMenuItem.TrySetPendingValue / ConfigMenuPage.Save", ProductCallback = "registered SaveConfig, WriteConfig, Configure, RefreshBindings", Path = settings.ZoomConfigPath, Persisted = File.ReadAllText(settings.ZoomConfigPath), Pending = page.HasPendingChanges, VisibleTitleButtonClaim = false });
            }
            else if (command == "focus:open")
            {
                focusState = DolocAPI.EnterUI<DolocTown.InputNameUiState>(state => state.HandleStartUpArgs("Zoom input focus observation", "", text => Write("unexpected-text-confirm.json", new { Text = text, NativeArchiveMutation = false }), 32));
                focusPanel = (DolocTown.UI.InputNameBox)ReadField(focusState, "panel")!;
                focusField = (InputField)ReadField(focusPanel, "inputField")!;
                focusField.onValueChanged.AddListener(ObserveFieldChange);
                focusField.onEndEdit.AddListener(ObserveFieldEndEdit);
                Write("focus-native-open.json", new { ProgrammaticNativeUiSetup = true, Entry = "DolocAPI.EnterUI<InputNameUiState> / HandleStartUpArgs", NativeState = focusState.GetType().FullName, Panel = focusPanel.GetType().FullName, Field = focusField.GetType().FullName, ConfirmationCallback = "evidence only; no archive mutation" });
            }
            else if (command == "focus:activate")
            {
                focusField!.Select(); focusField.ActivateInputField();
                Write("focus-setup.json", new { ProgrammaticNativeUiSetup = true, Field = focusField.name, EventSystem = EventSystem.current ? EventSystem.current.name : "missing", InputModule = EventSystem.current?.currentInputModule?.GetType().FullName });
            }
            else if (command == "focus:close")
            {
                BeginFocusClose("sequence cleanup");
            }
            else if (command == "quit") RequestQuit("sequence quit");
            else throw new Exception("Unsupported command; direct Zoom/position mutation is deliberately unavailable: " + command);
            Write("command-done.json", new { Command = command, Tick = ticks, Utc = DateTime.UtcNow });
        }
        private void AttachProductObservation()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "DTMAPI.Zoom");
            zoomType = assembly.GetType("DTMAPI.Zoom.ZoomCallbacks")!;
            zoom = zoomType.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) ?? throw new Exception("Zoom dependency must complete Entry first");
            var productEntry = assembly.GetType("DTMAPI.Zoom.ModEntry")!;
            guard.Patch(productEntry.GetMethod("OnKeybindPressed", BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(ObserveTyped))));
            foreach (string method in new[] { "SetViewScale", "ResetToVanilla" })
                guard.Patch(zoom.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic), prefix: new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(ObserveAction))));
            guard.Patch(productEntry.GetMethod("SaveConfig", BindingFlags.Instance | BindingFlags.NonPublic), postfix: new HarmonyMethod(typeof(ModEntry).GetMethod(nameof(ObserveConfigSaved))));
        }
        private IConfigMenuPage ConfigPage(string id) => (IConfigMenuPage)configRegistry!.GetType().GetMethod("GetPageCore", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(configRegistry, new object[] { id });
        private static object? ReadField(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                var field = type.GetField(name, flags); if (field != null) return field.GetValue(instance);
                var property = type.GetProperty(name, flags); if (property != null) return property.GetValue(instance);
            }
            throw new MissingFieldException(instance.GetType().FullName, name);
        }
        public static void ObserveConfigSaved(object __instance)
        {
            try
            {
                if (current == null) return;
                var registrations = (IInputRegistration[])ReadField(__instance, "registrations")!;
                current.Append("config-callbacks.jsonl", new { Tick = current.ticks, Utc = DateTime.UtcNow, Registrations = registrations.Select(r => new { r.Id, r.OwnerId, r.Scope, r.IsDisposed, Keys = r.Keybinds.ToString() }).ToArray(), ObservationOnly = true });
            }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
        public static void ObserveTyped(KeybindPressedEventArgs e)
        {
            try { if (current != null) current.Append("typed-input.jsonl", new { Utc = DateTime.UtcNow, Tick = current.ticks, Frame = Time.frameCount, e.OwnerId, e.KeybindId, e.TriggerButton, Bindings = e.Keybinds.ToString(), ScaleBefore = current.Read("CurrentViewScale"), Text = current.TextFocus() }); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
        public static void ObserveAction(MethodBase __originalMethod, object[] __args)
        {
            try { if (current != null) current.Append("product-actions.jsonl", new { Utc = DateTime.UtcNow, Tick = current.ticks, Frame = Time.frameCount, Method = __originalMethod.Name, Arguments = __args, ScaleBefore = current.Read("CurrentViewScale"), ObservationOnly = true }); }
            catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
        private void ObserveTextCharacter(char value) => Append("text-events.jsonl", new { Event = "Keyboard.onTextInput", Character = value.ToString(), CodePoint = (int)value, Tick = ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Text = TextFocus() });
        private void ObserveImeComposition(IMECompositionString value)
        { imeComposition = value.ToString(); Append("text-events.jsonl", new { Event = "Keyboard.onIMECompositionChange", Composition = imeComposition, Tick = ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Text = TextFocus() }); }
        private void ObserveFieldChange(string value) => Append("text-events.jsonl", new { Event = "InputField.onValueChanged", Value = value, Tick = ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Text = TextFocus() });
        private void ObserveFieldEndEdit(string value)
        { ++endEditEvents; Append("text-events.jsonl", new { Event = "InputField.onEndEdit", Value = value, Tick = ticks, Frame = Time.frameCount, Utc = DateTime.UtcNow, Text = TextFocus() }); }
        private void RequestQuit(string reason)
        {
            if (quitRequested) return;
            quitRequested = true; BeginFocusClose(reason);
        }
        private void BeginFocusClose(string reason)
        {
            if (closePhase != 0 || focusClosed) return;
            if (focusState == null) { focusClosed = true; Write("focus-close.json", new { Closed = true, Reason = reason, NoOwnedUi = true }); return; }
            if (!ReferenceEquals(DolocAPI.userInput.CurrentState, focusState)) throw new Exception("Refuse to close another native state");
            closeReason = reason; closeStartFrame = Time.frameCount; closeDeadline = Time.realtimeSinceStartup + 8; closePhase = 1; closeNeedsEndEdit = focusField && focusField!.isFocused;
            Write("focus-close-start.json", new { Reason = reason, Frame = closeStartFrame, EndEditEvents = endEditEvents, Text = TextFocus() });
            focusField!.DeactivateInputField();
        }
        private void AdvanceFocusClose()
        {
            if (closePhase <= 0 || focusClosed) return;
            if (Time.realtimeSinceStartup > closeDeadline) { closePhase = -1; Write("focus-close-failure.json", new { Frame = Time.frameCount, Reason = "normal close deadline", Text = TextFocus() }); throw new Exception("Owned native text UI did not finish normal close; refusing unsafe application quit"); }
            if (closePhase == 1 && Time.frameCount >= closeStartFrame + 3)
            {
                if (!ReferenceEquals(DolocAPI.userInput.CurrentState, focusState)) throw new Exception("Native state changed during own text UI cleanup; refusing unrelated PopState");
                if (focusField!.isFocused || (closeNeedsEndEdit && !(bool)ReadField(focusPanel!, "editEnd")!)) return;
                Write("focus-end-edit-settled.json", new { Frame = Time.frameCount, FramesSinceDeactivate = Time.frameCount - closeStartFrame, EndEditEvents = endEditEvents, NativeEditEnd = ReadField(focusPanel!, "editEnd"), Text = TextFocus() });
                DolocAPI.userInput.PopState(); closePhase = 2; closeStartFrame = Time.frameCount;
            }
            if (closePhase == 2 && Time.frameCount >= closeStartFrame + 3 && !ReferenceEquals(DolocAPI.userInput.CurrentState, focusState) && !focusPanel!.isRender && !focusPanel.inAnimation && !focusField!.isFocused && !focusField.gameObject.activeInHierarchy)
            {
                focusClosed = true; closePhase = 0;
                Write("focus-close.json", new { Closed = true, Reason = closeReason, Frame = Time.frameCount, FramesSincePop = Time.frameCount - closeStartFrame, EndEditEvents = endEditEvents, NativeNormal = DolocAPI.IsNormalState, Text = TextFocus() });
            }
        }
        private object TextFocus() => new {
            Present = (bool)focusField, Focused = focusField && focusField!.isFocused, Active = focusField && focusField!.gameObject.activeInHierarchy,
            Text = focusField ? focusField!.text : "", Selected = EventSystem.current?.currentSelectedGameObject?.name ?? "",
            SelectedExactField = focusField && EventSystem.current && ReferenceEquals(EventSystem.current!.currentSelectedGameObject, focusField!.gameObject),
            OwnedNativeState = focusState != null && ReferenceEquals(DolocAPI.userInput?.CurrentState, focusState),
            PanelRendering = focusPanel && focusPanel!.isRender, PanelAnimating = focusPanel && focusPanel!.inAnimation,
            FieldType = focusField?.GetType().FullName, ContentType = focusField?.contentType.ToString(), CharacterValidation = focusField?.characterValidation.ToString(), ReadOnly = focusField?.readOnly,
            ImeSelected = textKeyboard != null && textKeyboard.imeSelected.isPressed, ImeComposition = imeComposition,
            DeviceType = DolocAPI.UserInput?.DeviceType.ToString(), Locale = focusField ? DolocAPI.CurrentL10nId : null, EndEditEvents = endEditEvents,
            NativeState = DolocAPI.userInput?.CurrentState?.GetType().FullName, InputScope = inputDiagnostics?.Snapshot.Scope.ToString(), InputAudience = inputDiagnostics?.Snapshot.Audience.ToString() };
        private void Append(string file, object value) => File.AppendAllText(Path.Combine(settings.Evidence, file), JsonConvert.SerializeObject(value) + "\n");
        private object? Read(string name) => zoom?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(zoom);
        private void Snapshot(string label)
        {
            string name = (++sequence).ToString("D3") + "-" + label;
            var camera = DolocAPI.mainCamera;
            var rows = new List<object>();
            var active = new HashSet<SpriteRenderer>();
            var records = (IEnumerable?)zoom?.GetType().GetField("sprites", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(zoom);
            if (records != null) foreach (object hold in records)
            {
                var fields = BindingFlags.Instance | BindingFlags.NonPublic;
                var renderer = (SpriteRenderer)hold.GetType().GetField("Renderer", fields)!.GetValue(hold);
                if (!renderer) continue;
                active.Add(renderer);
                Vector3 baseline = (Vector3)hold.GetType().GetField("Baseline", fields)!.GetValue(hold);
                if (!observed.Any(o => ReferenceEquals(o.Renderer, renderer) && ReferenceEquals(o.Sprite, renderer.sprite) && o.Baseline == baseline))
                    observed.Add(new ObservedSprite { Renderer = renderer, Sprite = renderer.sprite, Baseline = baseline, Id = renderer.GetInstanceID(), Name = renderer.name, FirstSeen = name });
            }
            foreach (var observedSprite in observed)
            {
                var renderer = observedSprite.Renderer;
                if (!renderer) { rows.Add(new { observedSprite.Id, observedSprite.Name, observedSprite.FirstSeen, Alive = false }); continue; }
                rows.Add(new { observedSprite.Id, observedSprite.Name, observedSprite.FirstSeen, Alive = true, SameSprite = ReferenceEquals(observedSprite.Sprite, renderer.sprite), HeldByProduct = active.Contains(renderer), Sprite = renderer.sprite ? renderer.sprite.name : "", SpriteSize = renderer.sprite ? V(renderer.sprite.bounds.size) : null, Scale = V(renderer.transform.localScale), Position = V(renderer.transform.position), Color = renderer.color.ToString(), Enabled = renderer.enabled, Active = renderer.gameObject.activeInHierarchy, Baseline = V(observedSprite.Baseline), RestoredXY = renderer.transform.localScale.x == observedSprite.Baseline.x && renderer.transform.localScale.y == observedSprite.Baseline.y, PreservedZ = renderer.transform.localScale.z == observedSprite.Baseline.z, Shader = renderer.sharedMaterial ? renderer.sharedMaterial.shader.name : "" });
            }
            Write(name + ".json", new { label, ticks, loads, saves, Utc = DateTime.UtcNow, Frame = Time.frameCount, ApplicationFocused = Application.isFocused, NativeNormal = DolocAPI.IsNormalState, Text = TextFocus(), Room = DolocAPI.CurrentRoom?.RoomId, RoomType = DolocAPI.CurrentRoom?.GetType().FullName, AgentPosition = DolocAPI.CurrentRoom == null ? "" : DolocAPI.AgentPosition.ToString("F4"), Camera = camera ? camera.GetInstanceID() : 0, Size = camera ? camera.orthographicSize : 0, Position = camera ? camera.transform.position.ToString("F4") : "", Aspect = camera ? camera.aspect : 0, GameplayCamSize = DolocAPI.cameraController ? DolocAPI.cameraController.CamSize.ToString("F4") : "", Resolution = Screen.width + "x" + Screen.height, Scale = Read("CurrentViewScale"), Baseline = Read("VanillaOrthographicSize"), Pending = Read("PendingRestoreCount"), Status = Read("Status"), Discovery = Read("DiscoveryCount"), Writes = Read("ScaleWriteCount"), Sprites = rows });
            ScreenCapture.CaptureScreenshot(Path.Combine(settings.Evidence, name + ".png"));
        }
        private void Write(string file, object value) => File.WriteAllText(Path.Combine(settings.Evidence, file), JsonConvert.SerializeObject(value, Formatting.Indented));
        private static object V(Vector3 value) => new { value.x, value.y, value.z };
        private sealed class ObservedSprite { internal SpriteRenderer Renderer = null!; internal Sprite Sprite = null!; internal Vector3 Baseline; internal int Id; internal string Name = "", FirstSeen = ""; }
        public void Dispose()
        {
            Write("observer-dispose.json", new { loads, saves, ticks, Scale = Read("CurrentViewScale"), Pending = Read("PendingRestoreCount"), Patches = Read("InstalledPatchCount"), NativeSize = DolocAPI.mainCamera ? DolocAPI.mainCamera.orthographicSize : 0 });
            if (textKeyboard != null) { textKeyboard.onTextInput -= ObserveTextCharacter; textKeyboard.onIMECompositionChange -= ObserveImeComposition; }
            if (focusField) { focusField!.onValueChanged.RemoveListener(ObserveFieldChange); focusField.onEndEdit.RemoveListener(ObserveFieldEndEdit); }
            guard.UnpatchSelf(); Application.runInBackground = oldBackground; current = null;
        }
    }
    public sealed class Settings { public string Evidence { get; set; } = ""; public string ExpiresUtc { get; set; } = ""; public string ZoomConfigPath { get; set; } = ""; }
}

