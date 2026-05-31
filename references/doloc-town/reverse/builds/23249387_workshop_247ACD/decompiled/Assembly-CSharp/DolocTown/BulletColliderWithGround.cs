using UnityEngine;

namespace DolocTown;

public class BulletColliderWithGround : DolocObject
{
	private BulletEntity bulletEntity;

	private Collider2D _collider2D;

	protected override void __Init()
	{
		base.__Init();
		bulletEntity = GetComponentInParent<BulletEntity>();
		_collider2D = GetComponent<Collider2D>();
	}

	public bool IsCollidingWithWall()
	{
		return _collider2D.IsTouchingLayers(DolocAPI.gameConfig.groundMask);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!(bulletEntity == null) && BattleUtils.IsWall(other))
		{
			bulletEntity.callbackOnHit?.Invoke(other);
		}
	}
}
