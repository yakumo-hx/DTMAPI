using UnityEngine;

namespace DolocTown;

public class StaticTarget : InteractableObject, IBulletTrackingObject
{
	[SerializeField]
	private float defense = 2f;

	private Shiner _shiner;

	private Shaker _shaker;

	bool IBulletTrackingObject.isValid => true;

	Transform IBulletTrackingObject.transform => base.transform;

	protected override void __Init()
	{
		base.__Init();
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		_shiner = new Shiner(component);
		_shaker = new Shaker(base.transform);
	}

	public override bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		int value = BattleUtils.CalcDamage(attack, defense, criticalRate);
		_shaker.Shake();
		_shiner.Raise(LocMaterials.GAME_MAT_HIT, 0.1f);
		DolocAPI.RaiseInstantPSEffects(pos, InstantParticleEffectsType.TARGET_PARTS_SM);
		DolocAPI.RaiseDamageTip(value, pos);
		return true;
	}
}
