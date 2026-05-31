using UnityEngine;

namespace DolocTown.GameServiceLocator;

public interface IGameConfigProvider
{
	string VoidSceneName { get; }

	Vector2 VoidScenePosition { get; }

	LayerMask walkableMask { get; }

	LayerMask waterMask { get; }

	LayerMask groundMask { get; }

	LayerMask motorReboundMask { get; }

	LayerMask agentMask { get; }

	LayerMask enemyMask { get; }

	LayerMask EnemyFireTestMask => (int)groundMask | (int)agentMask;

	LayerMask DroneFireTestMask => (int)groundMask | (int)enemyMask;

	PhysicsMaterial2D MonsterPhysicsMaterial2D { get; }

	AnimationCurve MalignantWeatherLossCurve { get; }

	float BulletHitWallDurationThreshold { get; }
}
