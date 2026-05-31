namespace DolocTown;

public class BlockadeWall : InteractableObject
{
	public AirWall airWall;

	protected override void __Init()
	{
		base.__Init();
		airWall.showTips = false;
		airWall.gameObject.SetActive(value: false);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		airWall.gameObject.SetActive(value: true);
	}
}
