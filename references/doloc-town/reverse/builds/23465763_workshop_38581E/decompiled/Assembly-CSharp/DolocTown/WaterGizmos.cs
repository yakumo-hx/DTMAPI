using System.Linq;
using UnityEngine;

namespace DolocTown;

public class WaterGizmos : MonoBehaviour
{
	[SerializeField]
	private WaterController water;

	[SerializeField]
	[Min(0.1f)]
	private float gizmosSize = 0.1f;

	[SerializeField]
	private Color gizmosColor = Color.blue;

	[SerializeField]
	private Transform leftPoint;

	[SerializeField]
	private Transform rightPoint;

	[SerializeField]
	[Min(2f)]
	private int forceCount = 4;

	[SerializeField]
	private float floatingScale = 1f;

	[SerializeField]
	private GameObject floatingObject;

	private bool isInitialized;

	private Vector3[] forcePoints;

	private void Init()
	{
		isInitialized = true;
		forcePoints = new Vector3[forceCount];
		float num = Mathf.Abs(rightPoint.position.x - leftPoint.position.x);
		float num2 = num / (float)(forceCount - 1);
		_ = leftPoint.position;
		float num3 = (float)(forceCount - 1) * 0.5f;
		for (int i = 0; i < forceCount; i++)
		{
			forcePoints[i] = leftPoint.position + new Vector3(num2 * (float)i, 0f);
			forcePoints[i].z = Mathf.Abs((float)i - num3) / num3;
		}
	}

	private float[] CalcFloating()
	{
		float[] source = forcePoints.Select((Vector3 x) => water.CalcDistanceError(x) * x.z).ToArray();
		float avg = source.Average();
		return source.Select((float x) => x - avg).ToArray();
	}

	private void OnDrawGizmos()
	{
		if (isInitialized && (object)water != null)
		{
			Gizmos.color = gizmosColor;
			float[] array = CalcFloating();
			for (int i = 0; i < array.Length; i++)
			{
				Gizmos.DrawLine(forcePoints[i], (Vector2)forcePoints[i] + Vector2.up * array[i]);
			}
		}
	}
}
