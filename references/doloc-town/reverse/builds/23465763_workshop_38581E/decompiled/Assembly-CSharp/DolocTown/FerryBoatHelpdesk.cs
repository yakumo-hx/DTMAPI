using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

public class FerryBoatHelpdesk : InteractableObject
{
	[SerializeField]
	private FerryBoat ferryBoat;

	protected override void OnTouch()
	{
		base.OnTouch();
		if (CheckValid())
		{
			ShowTip(DolocConfig.StaticTexts.UiOperationCallBoat);
		}
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (CheckValid())
		{
			ferryBoat.TakeBoat();
		}
	}

	private bool CheckValid()
	{
		if (ferryBoat != null)
		{
			return Vector3.Distance(DolocAPI.AgentPosition, ferryBoat.position) > 15f;
		}
		return false;
	}
}
