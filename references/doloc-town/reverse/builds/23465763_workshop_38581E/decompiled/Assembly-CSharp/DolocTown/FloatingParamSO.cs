using UnityEngine;

namespace DolocTown;

[CreateAssetMenu(menuName = "多洛可小镇/浮动物体参数")]
public class FloatingParamSO : ScriptableObject
{
	[SerializeField]
	[Min(0.1f)]
	public float springConst = 6f;

	[SerializeField]
	public float dropVelocity = -8f;

	[SerializeField]
	public float floatVelocity = 0.8f;

	[SerializeField]
	[Min(0.001f)]
	public float damping = 0.1f;

	[SerializeField]
	public float dstClip = 0.5f;

	[SerializeField]
	[Min(1f)]
	public float objectWidth = 1.6f;

	[SerializeField]
	[Min(3f)]
	public int forcePointCount = 15;
}
