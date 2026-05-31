using UnityEngine;

namespace DolocTown;

[CreateAssetMenu(fileName = "WindControllerConfig", menuName = "多洛可小镇/天气系统/风力控制器配置")]
public class WindControllerConfig : ScriptableObject
{
	[SerializeField]
	private Vector2 interval = new Vector2(3f, 8f);

	[SerializeField]
	private Vector2 speed = new Vector2(5f, 10f);

	[SerializeField]
	private Vector2 duration = new Vector2(3f, 5f);

	public float randomInterval => Random.Range(interval.x, interval.y);

	public float randomSpeed => Random.Range(speed.x, speed.y);

	public float randomDuration => Random.Range(duration.x, duration.y);
}
