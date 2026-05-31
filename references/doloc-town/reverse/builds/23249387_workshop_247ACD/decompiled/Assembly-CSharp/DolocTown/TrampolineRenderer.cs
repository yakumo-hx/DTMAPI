using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/trampoline", DolocGameAssets.GAME_ENTITY_TRAMPOLINE)]
public class TrampolineRenderer : GameEntity
{
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private OnewayColliderRenderer onewayColliderRenderer;

	private Tween anim;

	private Shiner shiner;

	protected override void __Init()
	{
		base.__Init();
		shiner = new Shiner(spriteRenderer);
		onewayColliderRenderer.Init();
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		anim?.Kill();
		anim = base.transform.DOShakePosition(duration, new Vector3(shakeStrength, shakeStrength, 0f));
	}

	public void Shiner(float time)
	{
		shiner.Raise(LocMaterials.GAME_MAT_HOLOGRAM, time);
	}
}
