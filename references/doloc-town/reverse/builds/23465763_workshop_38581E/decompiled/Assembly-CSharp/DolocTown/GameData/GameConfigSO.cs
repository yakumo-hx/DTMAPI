using DolocTown.GameServiceLocator;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/配置/全局游戏参数配置")]
public class GameConfigSO : ScriptableObject, IGameConfigProvider
{
	[SerializeField]
	private string _voidSceneName;

	[SerializeField]
	private Vector2 _voidScenePosition;

	[SerializeField]
	private LayerMask _walkableMask;

	[SerializeField]
	private LayerMask _groundMask;

	[SerializeField]
	private LayerMask _airWallMask;

	[SerializeField]
	private LayerMask _waterMask;

	[SerializeField]
	private LayerMask _agentMask;

	[SerializeField]
	private LayerMask _enemyMask;

	[SerializeField]
	private PhysicsMaterial2D _enemyCollisionMaterial;

	[SerializeField]
	private float _bulletHitWallDurationThreshold = 0.3f;

	[SerializeField]
	private AnimationCurve _malignantWeatherLossCurve = new AnimationCurve();

	public LayerMask walkableMask => _walkableMask;

	public LayerMask waterMask => _waterMask;

	public LayerMask groundMask => _groundMask;

	public LayerMask agentMask => _agentMask;

	public LayerMask enemyMask => _enemyMask;

	public LayerMask airWallMask => _airWallMask;

	public LayerMask motorReboundMask => (int)_airWallMask | (int)_groundMask;

	public string VoidSceneName => _voidSceneName;

	public Vector2 VoidScenePosition => _voidScenePosition;

	public AnimationCurve MalignantWeatherLossCurve => _malignantWeatherLossCurve;

	public float BulletHitWallDurationThreshold => _bulletHitWallDurationThreshold;

	public PhysicsMaterial2D MonsterPhysicsMaterial2D => _enemyCollisionMaterial;
}
