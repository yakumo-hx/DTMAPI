using System;
using UnityEngine;

namespace DolocTown.UI;

public abstract class DolocUiRecyclableObjectSelfCall<T> : DolocUiRecyclableObject where T : DolocUiRecyclableObject
{
	public Action<T> recycle { get; set; }

	public void Recycle()
	{
		if (recycle == null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			recycle(GetComponent<T>());
		}
	}
}
