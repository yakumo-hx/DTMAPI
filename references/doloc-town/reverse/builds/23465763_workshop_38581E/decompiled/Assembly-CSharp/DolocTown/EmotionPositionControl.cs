using System;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class EmotionPositionControl : MonoBehaviour
{
	[SerializeField]
	private Transform positionHandle;

	private bool customPositionHandle;

	public Func<Vector2> CustomPositionGetter { get; set; }

	public Vector2 EmotionPosition => (Vector2)base.transform.position + EmotionOffset;

	public Vector2 EmotionOffset
	{
		get
		{
			if (CustomPositionGetter != null)
			{
				return CustomPositionGetter();
			}
			if (positionHandle == null)
			{
				return Vector2.zero;
			}
			return positionHandle.localPosition;
		}
		set
		{
			if (!(positionHandle == null))
			{
				positionHandle.localPosition = value;
			}
		}
	}

	private void EmotionTest()
	{
		if (DolocAPI.IsGameInitialized)
		{
			DolocAPI.RaiseEmotion(base.transform, EmotionName.LOVE);
		}
	}

	private void ClearEmotion()
	{
		if (DolocAPI.IsGameInitialized)
		{
			DolocAPI.ClearEmotions(base.transform);
		}
	}
}
