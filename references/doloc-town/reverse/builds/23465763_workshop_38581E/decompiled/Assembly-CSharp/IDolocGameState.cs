using System;
using DolocTown;

public interface IDolocGameState
{
	bool EnableBasicTipInteract { get; }

	bool ForceShowBasicTip { get; }

	bool ForceHideBasicTip { get; }

	bool ForceShowQuickInventory { get; }

	bool ForceHideQuickInventory { get; }

	bool ForceShowOperationTip { get; }

	bool ForceHideOperationTip { get; }

	bool DisableUseItem { get; }

	bool ShouldPauseGame { get; }

	bool ShouldLateUpdate { get; }

	bool SupportCutscenes { get; }

	bool SupportFestival { get; }

	bool ShowPauseTip { get; }

	bool ShowOutline { get; }

	DolocInputType InputType { get; }

	Action DisposableEnter { get; set; }

	Action DisposableExit { get; set; }

	Action DisposableResume { get; set; }

	Action DisposablePause { get; set; }

	bool __enter();

	bool __exit();

	bool __pause();

	bool __resume();

	void OnUpdate(float deltaTime);

	void OnLateUpdate(float deltaTime);

	void OnFixedUpdate(float deltaTime);

	void OnInputDeviceChanged(DolocInputDeviceType type);

	void Startup();
}
