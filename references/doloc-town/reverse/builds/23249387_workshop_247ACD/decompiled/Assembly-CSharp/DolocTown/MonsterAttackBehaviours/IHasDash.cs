using DG.Tweening;

namespace DolocTown.MonsterAttackBehaviours;

public interface IHasDash
{
	float DashSpeed { get; }

	float DashDistance { get; }

	Ease DashEase { get; }

	float DashDuration => DashDistance / DashSpeed;

	string DashSound { get; }

	bool HasAfterImage { get; }
}
