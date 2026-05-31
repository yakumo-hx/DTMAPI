using UnityEngine;

namespace DolocTown;

public class FerryBoat : InteractableObject
{
	[SerializeField]
	public string disembarkLeftId;

	[SerializeField]
	public string disembarkRightId;

	[SerializeField]
	private float boatStopPosLeft;

	[SerializeField]
	private float boatStopPosRight;

	[SerializeField]
	private WaterHandle waterHandle;

	[SerializeField]
	public FerryBoatController boatController;

	[SerializeField]
	private FerryBoatHelpdesk leftHelpdesk;

	[SerializeField]
	private FerryBoatHelpdesk rightHelpdesk;

	private bool disableUpdate;

	protected override void __Init()
	{
		base.__Init();
		boatController.Init();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		HideTouchEffect();
		if (DolocAPI.CheckAvailableInCurrentState(null))
		{
			TakeBoat();
		}
	}

	public void TakeBoat()
	{
		new TakeBoatState(this).Startup();
		boatController.SetMoveRange(new Vector2(boatStopPosLeft, boatStopPosRight));
	}

	protected override void OnRender(Room room)
	{
		if (!disableUpdate)
		{
			base.transform.position = GetBoatTargetPosition();
			disableUpdate = true;
		}
	}

	public Vector3 GetBoatTargetPosition()
	{
		return new Vector2((Mathf.Abs(DolocAPI.AgentPosition.x - boatStopPosLeft) > Mathf.Abs(DolocAPI.AgentPosition.x - boatStopPosRight)) ? boatStopPosRight : boatStopPosLeft, waterHandle.GetCurrentWaterHeight() + 0.25f);
	}
}
