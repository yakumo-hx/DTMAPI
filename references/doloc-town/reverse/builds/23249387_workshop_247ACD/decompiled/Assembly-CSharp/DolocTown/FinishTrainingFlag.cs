namespace DolocTown;

public class FinishTrainingFlag : InteractableObject
{
	protected override void OnTouch()
	{
		base.OnTouch();
		DolocAPI.FinishTrainingDungeon(shouldFade: true);
	}
}
