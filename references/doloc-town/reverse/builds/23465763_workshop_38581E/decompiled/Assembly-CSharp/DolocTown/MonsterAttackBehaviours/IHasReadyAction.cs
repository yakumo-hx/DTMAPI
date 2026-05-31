namespace DolocTown.MonsterAttackBehaviours;

public interface IHasReadyAction
{
	bool HasReadyAction { get; }

	float ReadyDuration { get; }
}
