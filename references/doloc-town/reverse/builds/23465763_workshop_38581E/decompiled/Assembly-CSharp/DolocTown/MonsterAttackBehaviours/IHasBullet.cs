namespace DolocTown.MonsterAttackBehaviours;

public interface IHasBullet
{
	string BulletId { get; }

	string BulletMoverId { get; }

	float BulletSpeed { get; }

	float BulletDuration { get; }

	float BulletDistance => BulletSpeed * BulletDuration;

	bool DisableRecoil { get; }

	bool DisableShootEffects { get; }

	string FireSound { get; }
}
