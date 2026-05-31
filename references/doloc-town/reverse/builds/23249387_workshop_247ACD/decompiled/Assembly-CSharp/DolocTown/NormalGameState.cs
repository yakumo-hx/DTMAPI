using UnityEngine;

namespace DolocTown;

public class NormalGameState : DolocTownGameStateBase
{
	private bool __shouldRunBattleSys;

	public override bool ForceShowOperationTip => true;

	public override bool ForceShowBasicTip => true;

	public override bool ForceShowQuickInventory => true;

	public override bool SupportFestival => true;

	public AgentControllerState AgentController { get; private set; }

	public BattleSystem battleSystem { get; private set; }

	public NormalGameState(AgentControllerState agentController, Transform dungeonContainer, GameStateMachine userInput, bool shouldPauseGame, bool shouldLateUpdate, bool supportCutscenes)
		: base(userInput, DolocInputType.NORMAL, shouldPauseGame, shouldLateUpdate, supportCutscenes)
	{
		AgentController = agentController;
		battleSystem = new BattleSystem(dungeonContainer);
	}

	public void SetRoom(Room room)
	{
		if (room != null)
		{
			battleSystem.droneEnv.SetMonsterHost(room);
		}
		bool _shouldRunBattleSys = __shouldRunBattleSys;
		__shouldRunBattleSys = room != null;
		if (_shouldRunBattleSys != __shouldRunBattleSys && !__shouldRunBattleSys)
		{
			battleSystem.Dispose();
		}
	}

	public void StopBattleManager()
	{
		__shouldRunBattleSys = false;
		battleSystem.Dispose();
	}

	public override void OnUpdate(float deltaTime)
	{
		if (DolocAPI.IsDataLoaded)
		{
			UpdateAgent(deltaTime);
		}
		DolocAPI.GameProcessSystem.OnUpdate(deltaTime);
		if (__shouldRunBattleSys)
		{
			battleSystem.OnUpdate(deltaTime);
		}
	}

	protected virtual void UpdateAgent(float deltaTime)
	{
		AgentController.OnUpdateInNormalState(deltaTime);
	}

	public override void OnFixedUpdate(float deltaTime)
	{
		if (DolocAPI.IsDataLoaded)
		{
			AgentController.OnFixedUpdate(deltaTime);
		}
		if (__shouldRunBattleSys)
		{
			battleSystem.OnFixedUpdate(deltaTime);
		}
	}

	public override void OnEnter()
	{
		AgentController.OnResume();
		battleSystem.OnResumeGame();
	}

	public override void OnExit()
	{
		AgentController.SetAttackable(value: false);
		battleSystem.OnPauseGame();
	}

	public override void OnResume()
	{
		AgentController.OnResume();
		battleSystem.OnResumeGame();
	}

	public override void OnPause()
	{
		AgentController.OnPause();
		battleSystem.OnPauseGame();
	}

	public override void OnInputDeviceChanged(DolocInputDeviceType type)
	{
		AgentController.droneController.OnInputDeviceChanged(type);
		DolocAPI.dolocBuilder.OnInputDeviceChanged(type);
	}
}
