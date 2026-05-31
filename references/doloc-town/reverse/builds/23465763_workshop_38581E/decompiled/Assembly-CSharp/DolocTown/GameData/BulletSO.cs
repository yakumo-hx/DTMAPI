using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "Bullet", menuName = "多洛可小镇[地牢]/战斗系统/子弹")]
public class BulletSO : SerializedScriptableObject
{
	[SerializeField]
	public BulletDirectionType directionType;

	[SerializeField]
	private EffectsConfigSO hitEffects;

	[SerializeField]
	private EffectsConfigSO shootEffects;

	[SerializeField]
	private string animation;

	[SerializeField]
	private bool isCircleCollider;

	[SerializeField]
	private float colliderRadius = 0.1f;

	[SerializeField]
	private Vector2 colliderSize = Vector2.one;

	[SerializeField]
	private Vector2 colliderOffset = Vector2.zero;

	public bool CreateProto(out BulletProto proto)
	{
		proto = null;
		if (animation.IsNullOrEmpty())
		{
			return false;
		}
		IEffects effects2;
		if (!(hitEffects == null))
		{
			IEffects effects = hitEffects;
			effects2 = effects;
		}
		else
		{
			effects2 = IEffects.Empty;
		}
		IEffects effects3 = effects2;
		IEffects effects4;
		if (!(shootEffects == null))
		{
			IEffects effects = shootEffects;
			effects4 = effects;
		}
		else
		{
			effects4 = IEffects.Empty;
		}
		IEffects effects5 = effects4;
		proto = new BulletProto(base.name, animation, directionType, effects5, effects3, isCircleCollider, colliderRadius, colliderSize, colliderOffset);
		return true;
	}
}
