using UnityEngine;

namespace DolocTown;

public class NoticeBoard : InteractableObject
{
	[SerializeField]
	private GameObject tipObj;

	private BoardMissionManager manager => DolocAPI.archiveHandle.cityData.boardMissionManager;

	protected override void OnRender(Room room)
	{
		SetMissionTipState(manager.HasNewMission);
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		DolocAPI.EnterUI<BoardMissionUiState>();
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		SceneLight componentInChildren = GetComponentInChildren<SceneLight>();
		if (componentInChildren != null && componentInChildren.IsTurnOn)
		{
			componentInChildren.TurnOn();
		}
	}

	public void SetMissionTipState(bool state)
	{
		if (!base.archiveData.LoadLockState(base.lockObjectId))
		{
			tipObj.SetActive(state);
		}
	}
}
