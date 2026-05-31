namespace DolocTown;

public class GameStateNoInput : DolocTownGameStateBase
{
	public override bool EnableBasicTipInteract => false;

	public override bool ForceHideOperationTip => true;

	public GameStateNoInput(GameStateMachine userInput)
		: base(userInput, DolocInputType.NONE, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
	}

	public override void OnUpdate(float deltaTime)
	{
	}

	public override void OnEnter()
	{
		DolocAPI.SetSceneOperationTipEnabled(value: false);
	}

	public override void OnExit()
	{
		DolocAPI.SetSceneOperationTipEnabled(value: true);
	}
}
