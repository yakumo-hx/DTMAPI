using DolocTown.Config.Drone;
using DolocTown.Config.Resource;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneWeaponSword : DroneWeapon
{
	public static readonly Vector2 ReadyDistance = new Vector2(3f, 0f);

	private Vector2 readyStartPosition;

	private float readyDirection;

	private float readyTimer;

	private bool isReady;

	private float lastReadyProcess;

	public override bool IsSword => true;

	public override bool IsSwordReadyNow => isReady;

	private float ReadyProcess => readyTimer / weaponParams.ReloadDuration;

	public DroneWeaponSword(Drone drone, DroneWeaponInfo proto, DroneWeaponParams weaponParams)
		: base(drone, proto, weaponParams)
	{
		drone.renderer.SwordCollider.BindCallback(OnSwordHitSomething);
	}

	private void OnSwordHitSomething(Collider2D other, IAttackable attackable, Vector2 hitPosition)
	{
		int dmg = Mathf.RoundToInt((float)weaponParams.Attack * Mathf.Clamp(lastReadyProcess, 0.3f, 1f));
		dmg = drone.HandleDamage(dmg);
		bool flag = RandomUtils.Dice(base.FinalCriticalRate);
		switch (attackable.attackableType)
		{
		case AttackableType.Enemy:
		{
			attackable.OnAttacked(dmg, flag, hitPosition, out var isDead);
			AttackHitInfo value = AttackHitInfo.HitEnemy(hitPosition, isSuccess: true, flag, isDead, attackable, other);
			GameEventArgs<AttackHitInfo> args = new GameEventArgs<AttackHitInfo>(value);
			drone.SendMessage(DroneEventType.HitEnemy, args);
			if (isDead)
			{
				drone.SendMessage(DroneEventType.EnemyDead, new GameEventArgs<AttackHitInfo>(value));
			}
			break;
		}
		case AttackableType.None:
			attackable.OnAttacked(weaponParams.Attack, flag, hitPosition);
			HandleResource(dmg, other, hitPosition);
			break;
		}
	}

	private void HandleResource(int atk, Collider2D other, Vector2 hitPosition)
	{
		DungeonResourceRenderer component = other.GetComponent<DungeonResourceRenderer>();
		if (!(component == null) && component.DungeonResource != null)
		{
			DungeonResource dungeonResource = component.DungeonResource;
			DungeonResourceType resourceType = dungeonResource.ResourceType;
			if (resourceType == DungeonResourceType.WEEDS || resourceType == DungeonResourceType.WEEDS_SMALL)
			{
				bool levelMatch = 1 >= dungeonResource.currentLevelData.BulletLevelConstraint;
				dungeonResource._Fell(new ResourceFellData(levelMatch, 1, atk, hitPosition, shouldCounterBack: false, shouldRaiseToolTip: false));
			}
		}
	}

	public override void StartSwordReady(Vector2 direction)
	{
		if (!isReady)
		{
			isReady = true;
			readyTimer = 0f;
			readyStartPosition = drone.renderer.position;
			readyDirection = Mathf.Sign(direction.x);
			drone.renderer.SetFollowEnabled(v: false);
			drone.renderer.FaceRight = readyDirection > 0f;
		}
	}

	public override void SwordReady(float dt)
	{
		if (isReady && !drone.renderer.IsDash)
		{
			readyTimer += dt;
			if (readyTimer > weaponParams.ReloadDuration)
			{
				readyTimer = weaponParams.ReloadDuration;
			}
			drone.renderer.position2d = readyStartPosition - ReadyDistance * (readyDirection * ReadyProcess);
		}
	}

	public override void TrySwordAttack(Vector2 direction)
	{
		if (!isReady || drone.renderer.IsDash)
		{
			return;
		}
		lastReadyProcess = ReadyProcess;
		isReady = false;
		drone.renderer.SwordCollider.SetSwordColliderEnabled(value: true);
		float x = Mathf.Sign(direction.x);
		float dashDst = weaponParams.AttackDistance * Mathf.Lerp(0.3f, 1.5f, lastReadyProcess);
		drone.renderer.Dash(new Vector2(x, 0f), dashDst, weaponParams.MoveSpeed, 0.1f, delegate
		{
			if (lastReadyProcess > 0.5f)
			{
				weaponParams.RaiseEffects(drone.renderer.position2d);
			}
			drone.renderer.SwordCollider.SetSwordColliderEnabled(value: false);
		}, delegate
		{
			drone.SendMessage(DroneEventType.ReloadComplete, GameEventArgs.None);
		});
	}
}
