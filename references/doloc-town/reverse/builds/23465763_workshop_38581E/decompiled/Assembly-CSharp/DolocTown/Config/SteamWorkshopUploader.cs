using System;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;

namespace DolocTown.Config;

public class SteamWorkshopUploader
{
	private struct WorkshopL10nText
	{
		public string l10nId;

		public string title;

		public string description;

		public WorkshopL10nText(string l10NId, string title, string description)
		{
			l10nId = l10NId;
			this.title = title;
			this.description = description;
		}
	}

	private readonly CallResult<SteamUGCQueryCompleted_t> queryItemDetailsResult;

	private readonly CallResult<CreateItemResult_t> createItemResult;

	private readonly CallResult<SubmitItemUpdateResult_t> submitItemResult;

	private readonly Queue<WorkshopL10nText> pendingL10nTexts = new Queue<WorkshopL10nText>();

	private ModInfo pendingMod;

	private WorkshopUploadMode pendingUploadMode;

	private ulong pendingWorkshopId;

	private bool isSubmittingBaseUpdate;

	private Action<WorkshopUploadPlan> resolveUploadPlanCallback;

	private Action<WorkshopUploadResult> uploadCompletedCallback;

	public bool IsUploading => pendingMod != null;

	public bool IsResolvingUploadPlan => resolveUploadPlanCallback != null;

	private bool IsBusy
	{
		get
		{
			if (!IsUploading)
			{
				return IsResolvingUploadPlan;
			}
			return true;
		}
	}

	public SteamWorkshopUploader()
	{
		queryItemDetailsResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryItemDetails);
		createItemResult = CallResult<CreateItemResult_t>.Create(OnCreateItem);
		submitItemResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItem);
	}

	public void ResolveUploadPlan(ModInfo mod, Action<WorkshopUploadPlan> onResolved)
	{
		if (mod != null)
		{
			if (mod.workshopId == 0L)
			{
				onResolved?.Invoke(new WorkshopUploadPlan(WorkshopUploadMode.Create, 0uL));
				return;
			}
			if (!SteamManager.Initialized)
			{
				Debug.LogWarning("[MOD] Steam not initialized, fallback to new workshop item upload plan.");
				onResolved?.Invoke(new WorkshopUploadPlan(WorkshopUploadMode.Create, 0uL));
				return;
			}
			if (IsBusy)
			{
				Debug.LogWarning("[MOD] Workshop uploader is busy");
				return;
			}
			pendingWorkshopId = mod.workshopId;
			resolveUploadPlanCallback = onResolved;
			SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[1]
			{
				new PublishedFileId_t(mod.workshopId)
			}, 1u));
			queryItemDetailsResult.Set(hAPICall);
		}
	}

	public void UploadMod(ModInfo mod, WorkshopUploadPlan plan, Action<WorkshopUploadResult> onCompleted)
	{
		if (!SteamManager.Initialized)
		{
			Debug.LogError("[MOD] Steam not initialized!");
			onCompleted?.Invoke(new WorkshopUploadResult(plan?.mode ?? WorkshopUploadMode.Create, success: false, 0uL));
			return;
		}
		if (IsBusy)
		{
			Debug.LogWarning("[MOD] Workshop upload already running");
			onCompleted?.Invoke(new WorkshopUploadResult(plan?.mode ?? WorkshopUploadMode.Create, success: false, plan?.workshopId ?? 0));
			return;
		}
		if (mod == null || plan == null)
		{
			Debug.LogError("[MOD] Workshop upload failed: invalid upload request");
			onCompleted?.Invoke(new WorkshopUploadResult(plan?.mode ?? WorkshopUploadMode.Create, success: false, 0uL));
			return;
		}
		if (!Directory.Exists(mod.rootPath))
		{
			Debug.LogError("[MOD] Mod folder not found: " + mod.rootPath);
			onCompleted?.Invoke(new WorkshopUploadResult(plan.mode, success: false, plan.workshopId));
			return;
		}
		pendingMod = mod;
		pendingUploadMode = plan.mode;
		pendingWorkshopId = plan.workshopId;
		uploadCompletedCallback = onCompleted;
		Debug.Log($"[MOD] {pendingUploadMode} workshop mod: {mod.title}");
		if (pendingUploadMode == WorkshopUploadMode.Update)
		{
			if (pendingWorkshopId == 0L)
			{
				Debug.LogError("[MOD] Workshop update failed: missing workshop id");
				CompleteUpload(success: false, 0uL);
			}
			else
			{
				UploadUpdate(pendingWorkshopId, mod);
			}
		}
		else
		{
			SteamAPICall_t hAPICall = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeFirst);
			createItemResult.Set(hAPICall);
		}
	}

	private void OnQueryItemDetails(SteamUGCQueryCompleted_t result, bool ioFailure)
	{
		WorkshopUploadPlan obj = new WorkshopUploadPlan(WorkshopUploadMode.Create, 0uL);
		if (!ioFailure && result.m_eResult == EResult.k_EResultOK && result.m_unNumResultsReturned != 0 && SteamUGC.GetQueryUGCResult(result.m_handle, 0u, out var pDetails) && pDetails.m_eResult == EResult.k_EResultOK && pDetails.m_ulSteamIDOwner == SteamUser.GetSteamID().m_SteamID)
		{
			obj = new WorkshopUploadPlan(WorkshopUploadMode.Update, pDetails.m_nPublishedFileId.m_PublishedFileId);
		}
		else
		{
			Debug.Log($"[MOD] Workshop item {pendingWorkshopId} is not owned by current user or cannot be resolved, will create a new item.");
		}
		SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
		Action<WorkshopUploadPlan> action = resolveUploadPlanCallback;
		resolveUploadPlanCallback = null;
		pendingWorkshopId = 0uL;
		action?.Invoke(obj);
	}

	private void OnCreateItem(CreateItemResult_t result, bool ioFailure)
	{
		if (ioFailure || result.m_eResult != EResult.k_EResultOK)
		{
			Debug.LogError("[MOD] CreateItem failed: " + result.m_eResult);
			CompleteUpload(success: false, 0uL);
			return;
		}
		ulong publishedFileId = result.m_nPublishedFileId.m_PublishedFileId;
		Debug.Log("[MOD] Workshop item created: " + publishedFileId);
		pendingWorkshopId = publishedFileId;
		if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
		{
			string url = "steam://url/CommunityFilePage/" + publishedFileId;
			Debug.Log("[MOD] User must accept workshop agreement");
			Application.OpenURL(url);
		}
		UploadUpdate(publishedFileId, pendingMod);
	}

	private void UploadUpdate(ulong workshopId, ModInfo mod)
	{
		pendingL10nTexts.Clear();
		foreach (WorkshopL10nText item in BuildTextsToUpload(mod))
		{
			pendingL10nTexts.Enqueue(item);
		}
		isSubmittingBaseUpdate = true;
		UGCUpdateHandle_t uGCUpdateHandle_t = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), new PublishedFileId_t(workshopId));
		if (!SteamUGC.SetItemContent(uGCUpdateHandle_t, mod.rootPath))
		{
			Debug.LogWarning("[MOD] Failed to set workshop item content path: " + mod.rootPath);
		}
		if (pendingL10nTexts.Count > 0)
		{
			WorkshopL10nText text = pendingL10nTexts.Dequeue();
			if (!ApplyLocalizedText(uGCUpdateHandle_t, text))
			{
				CompleteUpload(success: false, workshopId);
				return;
			}
		}
		if (File.Exists(mod.previewPath) && !SteamUGC.SetItemPreview(uGCUpdateHandle_t, mod.previewPath))
		{
			Debug.LogWarning("[MOD] Failed to set workshop preview: " + mod.previewPath);
		}
		if (!SteamUGC.SetItemTags(uGCUpdateHandle_t, mod.tags))
		{
			Debug.LogWarning("[MOD] Failed to set workshop tags.");
		}
		SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(uGCUpdateHandle_t, "Update mod");
		submitItemResult.Set(hAPICall);
	}

	private IEnumerable<WorkshopL10nText> BuildTextsToUpload(ModInfo mod)
	{
		List<WorkshopL10nText> list = new List<WorkshopL10nText>();
		HashSet<string> uploadedLanguages = new HashSet<string>();
		string text = DolocConfig.Tables.TbLocalization.GetOrDefault(DolocAPI.CurrentL10nId)?.SteamL10nId;
		if (!string.IsNullOrEmpty(text))
		{
			TryAddText(list, uploadedLanguages, text, mod.title, mod.description);
		}
		string[] uploadL10nIds = ModManifest.GetUploadL10nIds();
		foreach (string l10nId in uploadL10nIds)
		{
			TryAddText(list, uploadedLanguages, l10nId, mod.manifest.GetL10nIdTitle(l10nId), mod.manifest.GetL10nIdDescription(l10nId));
		}
		return list;
	}

	private void TryAddText(List<WorkshopL10nText> textToUpload, HashSet<string> uploadedLanguages, string l10nId, string title, string description)
	{
		if (!string.IsNullOrEmpty(l10nId) && !uploadedLanguages.Contains(l10nId))
		{
			if (string.IsNullOrEmpty(title))
			{
				Debug.LogWarning("[MOD] Skip workshop localization '" + l10nId + "' because title is empty.");
				return;
			}
			uploadedLanguages.Add(l10nId);
			textToUpload.Add(new WorkshopL10nText(l10nId, title, description ?? string.Empty));
		}
	}

	private bool ApplyLocalizedText(UGCUpdateHandle_t handle, WorkshopL10nText text)
	{
		if (!SteamUGC.SetItemUpdateLanguage(handle, text.l10nId))
		{
			Debug.LogError("[MOD] Failed to set workshop update language: " + text.l10nId);
			return false;
		}
		if (!SteamUGC.SetItemTitle(handle, text.title))
		{
			Debug.LogError("[MOD] Failed to set workshop title for language '" + text.l10nId + "'.");
			return false;
		}
		if (!SteamUGC.SetItemDescription(handle, text.description))
		{
			Debug.LogError("[MOD] Failed to set workshop description for language '" + text.l10nId + "'.");
			return false;
		}
		return true;
	}

	private void SubmitNextLocalizedText(ulong workshopId)
	{
		if (pendingL10nTexts.Count == 0)
		{
			Debug.Log("[MOD] Workshop upload success!");
			CompleteUpload(success: true, workshopId);
			return;
		}
		WorkshopL10nText text = pendingL10nTexts.Dequeue();
		UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), new PublishedFileId_t(workshopId));
		isSubmittingBaseUpdate = false;
		if (!ApplyLocalizedText(handle, text))
		{
			CompleteUpload(success: false, workshopId);
			return;
		}
		Debug.Log($"[MOD] Submit workshop localized text: {text.l10nId}, remaining: {pendingL10nTexts.Count}");
		SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(handle, "Update workshop localization: " + text.l10nId);
		submitItemResult.Set(hAPICall);
	}

	private void OnSubmitItem(SubmitItemUpdateResult_t result, bool ioFailure)
	{
		ulong workshopId = ((result.m_nPublishedFileId.m_PublishedFileId != 0L) ? result.m_nPublishedFileId.m_PublishedFileId : pendingWorkshopId);
		if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement && workshopId != 0L)
		{
			Application.OpenURL("steam://url/CommunityFilePage/" + workshopId);
		}
		if (ioFailure || result.m_eResult != EResult.k_EResultOK)
		{
			Debug.LogError("[MOD] Workshop upload failed: " + result.m_eResult);
			CompleteUpload(success: false, workshopId);
			return;
		}
		if (isSubmittingBaseUpdate)
		{
			Debug.Log("[MOD] Workshop base update submitted successfully, continue uploading localized texts.");
		}
		DolocAPI.DelayFrame(delegate
		{
			SubmitNextLocalizedText(workshopId);
		});
	}

	private void CompleteUpload(bool success, ulong workshopId)
	{
		Action<WorkshopUploadResult> action = uploadCompletedCallback;
		WorkshopUploadResult obj = new WorkshopUploadResult(pendingUploadMode, success, workshopId);
		pendingL10nTexts.Clear();
		isSubmittingBaseUpdate = false;
		pendingMod = null;
		pendingUploadMode = WorkshopUploadMode.Create;
		pendingWorkshopId = 0uL;
		uploadCompletedCallback = null;
		action?.Invoke(obj);
	}
}
