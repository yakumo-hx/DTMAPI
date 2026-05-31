using UnityEngine;

namespace DolocTown;

public interface IBuilderState
{
	bool Construct { get; }

	Vector2Int BuilderCellPosition { get; }

	bool OnUpdate(float deltaTime);

	void RunBuilder(Item item);

	void ExitBuilder();

	bool ItemFilter(Item item);

	string[] GetOperateTip(DolocInputDeviceType type);

	void OnInputDeviceChanged(DolocInputDeviceType type);

	void PreciseMovement_JoyStick(float deltaTime);

	void OnUpdateMoveCamera(Vector2 delta);

	void SetBuilderCellPosition(Vector2Int pos);
}
