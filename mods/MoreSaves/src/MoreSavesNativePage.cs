using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.Config;
using DolocTown.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DTMAPI.MoreSaves
{
    // Only the native GameDataUiState/Panel pair is adapted. Grid indices always remain local.
    internal sealed partial class MoreSavesNativePage : IMoreSavesPageHost, IMoreSavesRenameHost
    {
        private readonly GameDataUiState state;
        private readonly object localSave;
        private readonly Func<string, string, string> translate;
        private readonly IMonitor monitor;
        private readonly MethodInfo pathMethod;
        private readonly Func<bool> ownsNative;
        private readonly MoreSavesArchiveStorage storage;
        private readonly MoreSavesArchiveRename rename;
        private readonly FieldInfo nativeIndex = AccessTools.Field(typeof(GameDataUiState), "currentIndex");
        private GameObject? pager;
        private Button? previous;
        private Button? next;
        private Text? pageText;
        private bool reopenOnRestore;
        internal MoreSavesPageSession Session { get; }
        internal GameDataPanel Panel { get; }
        internal bool Restoring { get; private set; }

        internal MoreSavesNativePage(GameDataUiState state, Func<bool> ownsNative, Func<string, string, string> translate, IMonitor monitor)
        {
            this.state = state;
            this.translate = translate;
            this.monitor = monitor;
            this.ownsNative = ownsNative;
            Panel = DolocAPI.uiSystem.GetEntity<GameDataPanel>(state);
            localSave = GetLocalSave();
            pathMethod = AccessTools.Method(localSave.GetType(), "GetDataFullPath", new[] { typeof(int) });
            Session = new MoreSavesPageSession(this, ownsNative);
            storage = new MoreSavesArchiveStorage(this);
            rename = new MoreSavesArchiveRename(this);
        }

        private static object GetLocalSave() => AccessTools.Field(DolocAPI.dataPersistenceManager.GetType(), "fileDataHandler").GetValue(DolocAPI.dataPersistenceManager);
        public string PathFor(int index) => (string)pathMethod.Invoke(localSave, new object[] { index });
        public string BackupPathFor(int index) => (string)AccessTools.Method(localSave.GetType(), "GetDataBackupPath", new[] { typeof(int) }).Invoke(localSave, new object[] { index });
        public bool OwnsStorage => ownsNative() && ReferenceEquals(localSave, GetLocalSave());
        public string Decode(string encoded) => (string)AccessTools.Method(localSave.GetType(), "TryDecryptData", new[] { typeof(string) }).Invoke(localSave, new object[] { encoded });
        public string CopyWithNativeIndex(string encoded, int targetIndex)
        {
            object[] args = { targetIndex, encoded, null! };
            AccessTools.Method(localSave.GetType(), "FixArchiveIndex", new[] { typeof(int), typeof(string), typeof(string).MakeByRefType() }).Invoke(localSave, args);
            return (string)args[2];
        }
        public bool DeleteNative(int index) => (bool)AccessTools.Method(localSave.GetType(), "DeleteGame", new[] { typeof(int) }).Invoke(localSave, new object[] { index });
        public bool CanRename => !DolocAPI.IsDataLoaded && ReferenceEquals(DolocAPI.userInput.CurrentState, state);
        public int NameLimit => DolocAPI.GlobalParameter.InputPlayerNameMaxLength;
        public string DefaultName => DolocAPI.GlobalParameter.PlayerDefaultName;
        public int BackupCount => DolocAPI.gameManager.backupCount;
        public string TempPathFor(int index) => (string)AccessTools.Method(localSave.GetType(), "GetDataTempPath").Invoke(localSave, new object[] { index });
        public string PrevPathFor(int index, int previous) => (string)AccessTools.Method(localSave.GetType(), "GetDataPrevPath").Invoke(localSave, new object[] { index, previous });
        public void ScrollBackups(int index) => AccessTools.Method(localSave.GetType(), "ScrollBackups").Invoke(localSave, new object[] { index, BackupCount });
        public string EncodeDocument(object document) => (string)AccessTools.Method(localSave.GetType(), "GetDataToStore").Invoke(localSave, new[] { document });
        public string SummaryName(MoreSavesArchive archive) => ((BaseArchiveData)archive.Data!).customPlayerName;
        public MoreSavesOperationResult Rename(MoreSavesArchive archive, string name, Func<bool> current) => rename.Rename(archive, name, current);
        public void RenderRenamed(int cell, MoreSavesArchive archive) => Panel.Render(cell, (BaseArchiveData)archive.Data!);

        public MoreSavesArchive Read(int index)
        {
            try
            {
                if (!ReferenceEquals(localSave, GetLocalSave())) throw new InvalidOperationException("Native save manager changed.");
                string before = MoreSavesFileIdentity.ReadStamp(PathFor(index), out bool exists);
                BaseArchiveData? data = exists ? ReadSummary(PathFor(index)) : null;
                string after = MoreSavesFileIdentity.ReadStamp(PathFor(index), out bool stillExists);
                if (before != after || exists != stillExists) throw new InvalidOperationException("Archive changed while reading its summary.");
                return new MoreSavesArchive(index, !exists ? ArchiveAvailability.Empty : data == null ? ArchiveAvailability.Unavailable : ArchiveAvailability.Ready, after, data);
            }
            catch (Exception ex)
            {
                monitor.Log("MoreSaves cannot read archive #" + (index + 1) + ": " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return new MoreSavesArchive(index, ArchiveAvailability.Unavailable, string.Empty);
            }
        }

        private BaseArchiveData? ReadSummary(string path)
        {
            BaseArchiveData? data = MoreSavesArchiveDocument.ReadSummary<BaseArchiveData>(Decode(File.ReadAllText(path)));
            string? version = data?.version;
            return DolocAPI.gameManager.gameOuterConfig.ignoreMinimumVersion ||
                (version != null && new Version(version) >= new Version(DolocAPI.gameManager.minimumSupportedVersion))
                ? data : null;
        }

        public bool Matches(MoreSavesArchive archive)
        {
            if (archive.Availability == ArchiveAvailability.Unavailable || archive.Identity.Length == 0) return false;
            try { return ReferenceEquals(localSave, GetLocalSave()) && MoreSavesFileIdentity.ReadStamp(PathFor(archive.Index), out _) == archive.Identity; }
            catch { return false; }
        }

        internal void Show()
        {
            EnsurePager();
            EnsureRenameInput();
            pager!.SetActive(true);
            renameRoot!.SetActive(true);
            Session.Open();
            DolocAPI.UIRaisePopUp();
            Panel.Show();
        }

        public void Render(int page, MoreSavesArchive[] archives)
        {
            Panel.Render(archives.Select(a => (BaseArchiveData)a.Data!).ToArray(), DolocAPI.Has087DemoData());
            for (int cell = 0; cell < archives.Length; cell++)
            {
                GameDataSlot slot = Panel.GetSlot(cell);
                slot.SetIndex(archives[cell].Index);
                if (archives[cell].Availability == ArchiveAvailability.Unavailable)
                    ((Text)AccessTools.Field(typeof(GameDataSlot), "emptyTxt").GetValue(slot)).text = T("archive.unavailable", "Unavailable");
            }
            previous!.interactable = page > 0;
            next!.interactable = page + 1 < MoreSavesPageSession.PageCount;
            pageText!.text = (page + 1) + " / " + MoreSavesPageSession.PageCount;
        }

        public void Select(int globalIndex, int cell, ArchiveAvailability availability)
        {
            nativeIndex.SetValue(state, globalIndex);
            Panel.selectedIndex = cell;
            Panel.HighLightButton(Panel.btnConfirm);
            Panel.SetConfirmText(availability == ArchiveAvailability.Unavailable ? T("archive.unavailable", "Unavailable") :
                availability == ArchiveAvailability.Empty ? DolocConfig.StaticTexts.GameDataStart : DolocConfig.StaticTexts.GameDataLoad);
            if (!Panel.GetSlot(cell).IsSelected) Panel.GetSlot(cell).Select();
        }

        internal void UpdateInput()
        {
            if (!Session.IsOpen || !DolocAPI.UserInput.BaseIsCancelPressed) return;
            if (nameInput != null && nameInput.isFocused) return;
            GameObject? selected = EventSystem.current?.currentSelectedGameObject;
            if (Panel.focusedOnFunctionButtons || (pager != null && selected != null && selected.transform.IsChildOf(pager.transform)) ||
                (renameRoot != null && selected != null && selected.transform.IsChildOf(renameRoot.transform)))
                Panel.GetSlot(Session.Cell).Select();
            else DolocAPI.userInput.PopState();
        }

        public void ConfirmLoad(MoreSavesArchive archive, Action confirmed)
        {
            DolocAPI.UIRaiseConfirm();
            if (archive.Data is BaseArchiveData data)
            {
                var changes = new ModChangeData(data.enabledModInfos, DolocAPI.modManager.EnabledMods);
                if (changes.hasChanged)
                {
                    DolocAPI.EnterUI<ModChangeListUiState>(dialog => dialog.HandleStartUpArgs(changes, confirmed, null, !changes.hasRemoved));
                    return;
                }
            }
            confirmed();
        }

        public void HandoffLoad(Action dispatch)
        {
            // Native menu transition; the request already owns its immutable target before these exits.
            DolocAPI.gameUiStates.GetState<HomePageUiState>().Kill();
            DolocAPI.userInput.PopState();
            if (DolocAPI.userInput.CheckState<HomePageUiState>()) DolocAPI.userInput.PopState();
            DolocAPI.uiSystem.GetEntity<LoadingPanel>().Show(true, () => DolocAPI.DelayFrame(dispatch));
        }

        public void Load(bool isNew, int globalIndex)
        {
            // Delegate native new/load/error handling, with an index captured before the loading transition.
            AccessTools.Method(typeof(GameDataUiState), "Load").Invoke(state, new object[] { isNew, globalIndex });
        }

        public void RejectLoad()
        {
            DolocAPI.HidePanel<LoadingPanel>();
            DolocAPI.EnterUI<HomePageUiState>();
            Notice("target.changed");
        }

        public void ConfirmDelete(int globalIndex, Action confirmed) => DolocAPI.ShowQuestionBox(
            DolocUtils.Format(DolocConfig.StaticTexts.GameDataDelConfirm, "#" + (globalIndex + 1)), confirmed, null, false);
        public MoreSavesOperationResult Delete(MoreSavesArchive archive, Func<bool> current) => storage.Delete(archive, current);
        public MoreSavesOperationResult Duplicate(MoreSavesArchive archive, Func<bool> current) => storage.Duplicate(archive, current);
        public void NoticeOperation(string operation, MoreSavesOperationResult result)
        {
            string suffix = result.Status == MoreSavesOperationStatus.Full ? "full" :
                result.Status == MoreSavesOperationStatus.InvalidName ? "invalid" :
                result.Status == MoreSavesOperationStatus.Unavailable ? "unavailable" :
                result.Status == MoreSavesOperationStatus.Changed ? "changed" :
                result.Status == MoreSavesOperationStatus.CommittedUnverified ? "uncertain" : "failed";
            monitor.Log("MoreSaves " + operation + " #" + (result.Index + 1) + ": " + result.Status + " " + result.Detail, LogLevel.Warn);
            DolocAPI.ShowMessageBoxSmall(T(operation + "." + suffix, operation + ": " + suffix) + " (#" + (result.Index + 1) + ")");
        }
        public void Notice(string key) => DolocAPI.ShowMessageBoxSmall(T(key, key));
        private string T(string key, string fallback) => translate(key, fallback);

        internal void Close() { Session.Close(); if (pager != null) pager.SetActive(false); if (renameRoot != null) renameRoot.SetActive(false); }

        internal void Suspend()
        {
            reopenOnRestore |= Session.IsOpen;
            Session.Retire();
            if (renameRoot != null) { renameRoot.SetActive(false); UnityEngine.Object.Destroy(renameRoot); renameRoot = null; nameInput = null; renameButton = null; }
            if (pager != null) { pager.SetActive(false); UnityEngine.Object.Destroy(pager); pager = null; }
        }

        internal void RestoreNative()
        {
            if (!reopenOnRestore || Panel == null) return;
            Restoring = true;
            try
            {
                nativeIndex.SetValue(state, 0);
                AccessTools.Method(typeof(GameDataUiState), "Show").Invoke(state, null);
                reopenOnRestore = false;
            }
            finally { Restoring = false; }
        }

        private void EnsurePager()
        {
            if (pager != null) return;
            pager = new GameObject("DTMAPI.MoreSaves.Paging", typeof(RectTransform), typeof(LayoutElement));
            // AutoSizeUIPanel's vertical layout otherwise recentres every new child.
            pager.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)pager.transform;
            rect.SetParent(Panel.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(260, 36);
            rect.anchoredPosition = new Vector2(-12, 12);
            Text style = Panel.btnConfirm.GetComponentInChildren<Text>(true);
            previous = MakeButton("Previous", "<", new Vector2(0, 0), style, () => Session.TurnPage(-1));
            next = MakeButton("Next", ">", new Vector2(200, 0), style, () => Session.TurnPage(1));
            pageText = MakeText("Page", style, pager.transform, new Vector2(65, 0), new Vector2(130, 36));
        }

        private Button MakeButton(string name, string caption, Vector2 position, Text style, Action clicked)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(pager!.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(60, 36);
            var source = Panel.btnConfirm.targetGraphic as Image;
            Image image = go.GetComponent<Image>();
            if (source != null) { image.sprite = source.sprite; image.type = source.type; image.color = source.color; }
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = Panel.btnConfirm.colors;
            button.onClick.AddListener(() => clicked());
            MakeText("Label", style, go.transform, Vector2.zero, rect.sizeDelta).text = caption;
            return button;
        }

        private static Text MakeText(string name, Text style, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = go.GetComponent<Text>();
            text.font = style.font;
            text.fontSize = style.fontSize;
            text.color = style.color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }
}
