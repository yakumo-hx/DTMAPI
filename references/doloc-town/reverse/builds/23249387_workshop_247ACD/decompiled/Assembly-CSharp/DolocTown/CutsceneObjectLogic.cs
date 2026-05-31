using DolocTown.Config;
using DolocTown.Config.Room;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class CutsceneObjectLogic : InteractableObjectLogic
{
	[JsonProperty]
	private int interactCount;

	[JsonProperty]
	private bool refreshedAfterInteract;

	private DialogueObjectInfo _config;

	[JsonProperty]
	public string dialogueObjectId { get; private set; }

	private DialogueObjectInfo config
	{
		get
		{
			if (_config == null)
			{
				_config = DolocConfig.Tables.TbDialogueObject.GetOrDefault(dialogueObjectId ?? "");
			}
			return _config;
		}
	}

	[JsonConstructor]
	public CutsceneObjectLogic(string roomId, string guid, string dialogueObjectId, int interactCount = -1, bool refreshedAfterInteract = true)
		: base(roomId, guid)
	{
		this.dialogueObjectId = dialogueObjectId ?? "";
		this.interactCount = interactCount;
		this.refreshedAfterInteract = refreshedAfterInteract;
		base.IsValid = config != null;
	}

	public bool TryGetCurrentDialogueNode(out string node)
	{
		node = string.Empty;
		string[] array = config?.DialogueSequence;
		if (config == null || array == null || array.IsNullOrEmpty())
		{
			return false;
		}
		int num = (config.LoopSequence ? (interactCount % array.Length) : Mathf.Clamp(interactCount, 0, array.Length - 1));
		node = array[num];
		return true;
	}

	public void TryAddInteractCount()
	{
		DolocAPI.archiveHandle.cityData.dialogueManager.Visit(base.ObjectId);
		if (base.IsValid && refreshedAfterInteract)
		{
			refreshedAfterInteract = false;
			interactCount++;
		}
	}

	protected override void OnDailyRefresh(bool isRender)
	{
		base.OnDailyRefresh(isRender);
		if (base.IsValid && config.RefreshType == ObjectRefreshFrequency.Daily)
		{
			ClearInteractState();
		}
	}

	protected override void OnWeeklyRefresh(bool isRender)
	{
		base.OnWeeklyRefresh(isRender);
		if (base.IsValid && config.RefreshType == ObjectRefreshFrequency.Weekly)
		{
			ClearInteractState();
		}
	}

	protected override void OnMonthlyRefresh(bool isRender)
	{
		base.OnMonthlyRefresh(isRender);
		if (base.IsValid && config.RefreshType == ObjectRefreshFrequency.Monthly)
		{
			ClearInteractState();
		}
	}

	protected override void OnYearlyRefresh(bool isRender)
	{
		base.OnYearlyRefresh(isRender);
		if (base.IsValid && config.RefreshType == ObjectRefreshFrequency.Yearly)
		{
			ClearInteractState();
		}
	}

	private void ClearInteractState()
	{
		refreshedAfterInteract = true;
		DolocAPI.archiveHandle.cityData.dialogueManager.ClearVisitedCount(base.ObjectId);
	}
}
