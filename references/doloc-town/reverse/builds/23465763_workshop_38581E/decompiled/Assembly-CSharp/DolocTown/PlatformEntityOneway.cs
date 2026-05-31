using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(EdgeCollider2D), typeof(PlatformEffector2D))]
[GameEntityManager("/farm/platform_collider", DolocGameAssets.GAME_ENTITY_PLATFORMCOLLIDER)]
public class PlatformEntityOneway : GameEntity
{
	[SerializeField]
	public bool ignoreMsg;

	public EdgeCollider2D ptCollider { get; private set; }

	public Platform Platform { get; set; }

	protected override void __Init()
	{
		base.__Init();
		ptCollider = GetComponent<EdgeCollider2D>();
	}

	public override void OnReuse()
	{
		base.OnReuse();
		base.gameObject.layer = LayerMask.NameToLayer("Platform");
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (Platform != null)
		{
			Platform.Collider = null;
			Platform = null;
		}
	}
}
