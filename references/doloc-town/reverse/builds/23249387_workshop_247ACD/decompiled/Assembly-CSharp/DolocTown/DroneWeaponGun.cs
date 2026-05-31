using System;
using DolocTown.Config.Drone;
using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class DroneWeaponGun : DroneWeapon
{
	private readonly BulletManager bulletManager;

	private readonly RSTimer timer = new RSTimer();

	private readonly RSTimer autoReloadTimer = new RSTimer(5f);

	private readonly RSTimer messageTipCounter = new RSTimer(3f);

	private readonly Func<Vector2> NearestMonsterGetter;

	private readonly GunReloadTip tipReload;

	private int currentClipCount;

	private bool isFree = true;

	private bool isReloading;

	public override bool IsGun => true;

	public override bool IsFireNotAvailable
	{
		get
		{
			if (!isReloading)
			{
				return !isFree;
			}
			return true;
		}
	}

	public bool IsFullClip => currentClipCount == weaponParams.ClipCapacity;

	public bool IsNotFullClip => currentClipCount < weaponParams.ClipCapacity;

	private float ReloadingEnergyCost
	{
		get
		{
			if (currentClipCount == 0)
			{
				return weaponParams.PowerCost;
			}
			int num = weaponParams.ClipCapacity - currentClipCount;
			return weaponParams.PowerCost * ((float)num / (float)weaponParams.ClipCapacity);
		}
	}

	public DroneWeaponGun(Drone drone, DroneWeaponInfo proto, DroneWeaponParams weaponParams, BulletManager bulletManager)
		: base(drone, proto, weaponParams)
	{
		this.bulletManager = bulletManager;
		timer.SetInterval(weaponParams.AttackInterval);
		currentClipCount = weaponParams.ClipCapacity;
		tipReload = DolocAPI.uiSystem.GetFromPoolInScene<GunReloadTip>();
		tipReload.SetVisible(value: false);
	}

	private void InvokeBullet(Vector2 pos, Vector2 dir, float moveSpeed, float duration, Action<Bullet, Collider2D> cb, Action<Bullet> bulletHandle = null, bool disableShootEffects = true)
	{
		if (bulletManager != null)
		{
			bulletManager.InvokeBullet(pos, dir, moveSpeed, duration, cb, delegate(Bullet bullet)
			{
				bulletHandle?.Invoke(bullet);
				HandleBulletExtra(pos, bullet);
			}, disableShootEffects);
		}
	}

	private void HandleBulletExtra(Vector2 shootPosition, Bullet bullet)
	{
		Room currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom != null)
		{
			Vector2Int cellPos = currentRoom.Geometry.CalcCellPosition(shootPosition);
			if (currentRoom.Geometry.IsObstacle(cellPos) || bullet.IsCollidingWithWall())
			{
				bullet.DisableWallCollisionForSecs(0.1f);
			}
		}
	}

	private void OnShoot(Vector2 pos, Vector2 dir, bool shouldSendMessage = true, bool shouldNoise = true, bool shouldRaiseSound = true)
	{
		if (shouldNoise)
		{
			dir = dir.Noise(weaponParams.Accuracy);
		}
		weaponParams.RaiseEffects(pos);
		if (weaponParams.extraBullets > 0)
		{
			foreach (Vector2 item in dir.SplitIntoSector(weaponParams.extraBullets + 1, weaponParams.extraBulletAngle))
			{
				InvokeBullet(pos, item, weaponParams.MoveSpeed, weaponParams.BulletDuration, OnBulletHit, drone.HandleBullet, disableShootEffects: false);
			}
		}
		else
		{
			InvokeBullet(pos, dir, weaponParams.MoveSpeed, weaponParams.BulletDuration, OnBulletHit, drone.HandleBullet, disableShootEffects: false);
		}
		AttackInfo value = new AttackInfo(pos, dir);
		if (shouldSendMessage)
		{
			drone.SendMessage(DroneEventType.Shoot, new GameEventArgs<AttackInfo>(value));
			DolocAPI.Broadcast(OperationEventType.FIRE_DRONE);
		}
		if (shouldRaiseSound)
		{
			DolocAPI.Sound.PostSoundEvent(proto.AttackSoundEvent);
		}
	}

	public void ShootByCurrentGun(BulletManager bulletManager, Vector2 dir)
	{
		Vector2 shootPosition = drone.renderer.ShootPosition;
		weaponParams.RaiseEffects(shootPosition);
		DolocAPI.Sound.PostSoundEvent(proto.AttackSoundEvent);
		InvokeBullet(shootPosition, dir, weaponParams.MoveSpeed, weaponParams.BulletDuration, OnBulletHit);
	}

	public void OnBulletHit(Bullet bullet, Collider2D other)
	{
		int attack = drone.HandleDamage(weaponParams.Attack);
		AttackHitInfo attackHitInfo = BattleUtils.OnBulletHitEnemy(bulletManager, attack, base.FinalCriticalRate, bullet, other);
		if (!attackHitInfo.IsHitEnemy)
		{
			if (attackHitInfo.isWall)
			{
				drone.HandleCollisionWithWall(bullet, attackHitInfo);
				return;
			}
			DroneFunctionCollectorHelper.HandleResource(bullet, other);
			DroneFunctionFarmCollectorHelper.HandleCrop(bullet, other);
			return;
		}
		if (!drone.HandleCollisionWithEnemy(bullet, attackHitInfo))
		{
			bullet.Recycle();
		}
		GameEventArgs<AttackHitInfo> args = new GameEventArgs<AttackHitInfo>(attackHitInfo);
		drone.SendMessage(DroneEventType.HitEnemy, args);
		if (attackHitInfo.isDead)
		{
			drone.SendMessage(DroneEventType.EnemyDead, args);
		}
	}

	protected bool Attack(Vector2 pos, Vector2 dir, bool isAuto, bool shouldRaiseSound = true)
	{
		if (IsFireNotAvailable)
		{
			return false;
		}
		if (currentClipCount > 0)
		{
			autoReloadTimer.Reset();
			currentClipCount--;
			if (currentClipCount == 0)
			{
				Reload();
			}
			isFree = false;
			timer.Reset();
			OnShoot(pos, dir, shouldSendMessage: true, shouldNoise: true, shouldRaiseSound);
			return true;
		}
		if (isAuto)
		{
			if (drone.structure.Power >= weaponParams.PowerCost)
			{
				Reload();
			}
		}
		else
		{
			Reload();
		}
		return false;
	}

	public override void GunAttackForce(Vector2 direction, bool shouldRaiseSound = true)
	{
		Vector2 shootPosition = drone.renderer.ShootPosition;
		OnShoot(shootPosition, direction, shouldSendMessage: false, shouldNoise: false, shouldRaiseSound);
	}

	public override bool GunAttack(Vector2 direction, bool isAuto = false, bool shouldRaiseSound = true)
	{
		Vector2 shootPosition = drone.renderer.ShootPosition;
		return Attack(shootPosition, direction, isAuto, shouldRaiseSound);
	}

	public override void Dispose()
	{
		DolocAPI.battleSystem.RecycleBulletManager(bulletManager);
		DolocAPI.uiSystem.RecycleToPoolInScene(tipReload);
	}

	public override void Reset()
	{
		currentClipCount = weaponParams.ClipCapacity;
		timer.SetInterval(weaponParams.AttackInterval);
	}

	private void OnReloadCompleted()
	{
		currentClipCount = weaponParams.ClipCapacity;
		drone.SendMessage(DroneEventType.ReloadComplete, GameEventArgs.None);
		DolocAPI.Broadcast(GameEventType.GUN_RELOAD_FINISH);
	}

	public override void OnUpdate(float dt)
	{
		if (isReloading)
		{
			tipReload.position = drone.renderer.reloadTipPosition;
			tipReload.process = timer.process;
		}
	}

	public override void OnFixedUpdate(float dt)
	{
		if (isReloading)
		{
			if (timer.Tick(dt))
			{
				isReloading = false;
				tipReload.SetVisible(value: false);
				timer.SetInterval(weaponParams.AttackInterval);
				OnReloadCompleted();
			}
		}
		else
		{
			if (IsNotFullClip && autoReloadTimer.Tick(dt))
			{
				Reload();
			}
			if (!isFree)
			{
				isFree = timer.Tick(dt);
			}
		}
	}

	public override void Reload()
	{
		if (currentClipCount == weaponParams.ClipCapacity || isReloading)
		{
			return;
		}
		if (!drone.TryCostPower(ReloadingEnergyCost))
		{
			if (messageTipCounter.Tick(Time.deltaTime))
			{
				DolocAPI.RaiseInstantAnimEffects(drone.renderer.position2d, InstAnimEffectType.LOW_POWER, Vector2.right, flip: false, LocMaterials.GAME_MAT_2D_UNLIT, "SceneUI");
			}
		}
		else
		{
			autoReloadTimer.Reset();
			timer.SetInterval(weaponParams.ReloadDuration);
			tipReload.SetVisible(value: true);
			isReloading = true;
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_GUN_RELOAD);
		}
	}
}
