using UnityEngine;

namespace DolocTown;

public class ExpressDroneDebug : MonoBehaviour
{
	[SerializeField]
	protected GameObject targetPoint;

	public void Launch()
	{
		GetComponent<ExpressDrone>()?.Launch(targetPoint.transform.position);
	}

	public void Reset()
	{
		GetComponent<ExpressDrone>()?.Reset();
	}

	public void Recycle()
	{
		GetComponent<ExpressDrone>()?.Recycle(targetPoint.transform.position);
	}
}
