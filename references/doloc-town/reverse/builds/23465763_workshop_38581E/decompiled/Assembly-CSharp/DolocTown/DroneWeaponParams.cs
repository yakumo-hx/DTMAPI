using UnityEngine;

namespace DolocTown;

public readonly struct DroneWeaponParams
{
	public readonly int Attack;

	public readonly float CriticalRate;

	public readonly float Accuracy;

	public readonly float AttackInterval;

	public readonly float PowerCost;

	public readonly float MoveSpeed;

	public readonly float BulletDuration;

	public readonly int ClipCapacity;

	public readonly float ReloadDuration;

	public readonly float AttackDistance;

	public readonly int extraBullets;

	public readonly float extraBulletAngle;

	private readonly bool shouldRaiseEffects;

	public readonly string[] EffectIds;

	public DroneWeaponParams(int attack, float criticalRate, float accuracy, float attackInterval, float powerCost, float moveSpeed, float shootDistance, int gunClipCapacity = 1, float gunReloadTime = 0f, int extraBullets = 0, float extraBulletAngle = 0f, string[] effectIds = null)
	{
		Attack = Mathf.Max(0, attack);
		CriticalRate = Mathf.Clamp01(criticalRate);
		Accuracy = Mathf.Clamp01(accuracy);
		AttackInterval = Mathf.Max(0f, attackInterval);
		PowerCost = Mathf.Max(0f, powerCost);
		MoveSpeed = Mathf.Max(1f, moveSpeed);
		BulletDuration = Mathf.Max(1f, shootDistance) / moveSpeed;
		ClipCapacity = Mathf.Max(1, gunClipCapacity);
		ReloadDuration = Mathf.Max(0f, gunReloadTime);
		AttackDistance = Mathf.Max(1f, shootDistance);
		shouldRaiseEffects = !effectIds.IsNullOrEmpty();
		this.extraBullets = Mathf.Max(0, extraBullets);
		this.extraBulletAngle = Mathf.Max(10f, extraBulletAngle);
		EffectIds = effectIds;
	}

	public void RaiseEffects(Vector2 position)
	{
		if (shouldRaiseEffects)
		{
			string[] effectIds = EffectIds;
			foreach (string effectsConfigName in effectIds)
			{
				DolocAPI.RaiseEffectsSO(position, effectsConfigName);
			}
		}
	}
}
