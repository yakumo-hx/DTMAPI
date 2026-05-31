using DolocTown.Config;
using DolocTown.Config.Equipment;
using UnityEngine;

namespace DolocTown;

public class InteractableSavePoint : InteractableObject
{
	[SerializeField]
	public SaveOptionType saveOptionType;

	[SerializeField]
	public Transform sitPosition;

	[SerializeField]
	public string chairConfig;

	protected override void __Init()
	{
		base.__Init();
		if (sitPosition != null)
		{
			sitPosition.gameObject.SetActive(value: false);
		}
	}

	protected override void OnInteract()
	{
		PushTip();
		switch (saveOptionType)
		{
		case SaveOptionType.Sleep:
			if (DolocAPI.CheckAvailableInCurrentState(null) && !DolocAPI.userInput.CurrentState.ShouldPauseGame)
			{
				DolocAPI.ShowSleepMenu(delegate
				{
					TrySendGameEvent();
				});
			}
			break;
		case SaveOptionType.KillTime:
		{
			Vector3 vector = sitPosition.position;
			vector.z = base.transform.position.y - vector.y - 0.001f;
			DisableOutline();
			ChairInfo orDefault = DolocConfig.Tables.TbChair.GetOrDefault(chairConfig);
			KillTimeState.EntryKillTimeState(new SitParams(base.transform.position, vector, orDefault), delegate
			{
				TrySendGameEvent();
			});
			break;
		}
		}
	}
}
