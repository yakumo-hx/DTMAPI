using UnityEngine;

namespace DolocTown.GameData;

public class BulletProto
{
	public readonly string name;

	public readonly string animation;

	public readonly bool isCircle;

	public readonly BulletDirectionType directionType;

	public readonly IEffects shootEffects;

	public readonly IEffects hitEffects;

	public readonly float colliderRadius;

	public readonly Vector2 colliderSize;

	public readonly Vector2 colliderOffset;

	public bool IsDirectional => directionType == BulletDirectionType.ANISOTROPIC;

	public BulletProto(string name, string animation, BulletDirectionType directionType, IEffects shootEffects, IEffects hitEffects, bool isCircle, float colliderRadius, Vector2 colliderSize, Vector2 colliderOffset)
	{
		this.name = name;
		this.animation = animation;
		this.directionType = directionType;
		this.shootEffects = shootEffects;
		this.hitEffects = hitEffects;
		this.isCircle = isCircle;
		this.colliderRadius = colliderRadius;
		this.colliderSize = colliderSize;
		this.colliderOffset = colliderOffset;
	}
}
