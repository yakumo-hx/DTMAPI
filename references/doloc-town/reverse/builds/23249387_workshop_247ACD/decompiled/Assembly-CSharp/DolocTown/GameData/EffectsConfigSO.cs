using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/特效", fileName = "NewEffects")]
public class EffectsConfigSO : SerializedScriptableObject, IEffects
{
	[SerializeField]
	private EffectsSO[] _effectsSos = Array.Empty<EffectsSO>();

	public void Raise(Vector2 pos)
	{
		if (!_effectsSos.IsNullOrEmpty())
		{
			EffectsSO[] effectsSos = _effectsSos;
			for (int i = 0; i < effectsSos.Length; i++)
			{
				effectsSos[i].Raise(pos);
			}
		}
	}

	public void Raise(Vector2 pos, Vector2 dir)
	{
		if (!_effectsSos.IsNullOrEmpty())
		{
			EffectsSO[] effectsSos = _effectsSos;
			for (int i = 0; i < effectsSos.Length; i++)
			{
				effectsSos[i].Raise(pos, dir);
			}
		}
	}

	public void DebugPlaying()
	{
		if (!DolocAPI.IsGameInitialized)
		{
			Debug.LogError("游戏未初始化");
		}
		else
		{
			Raise(DolocAPI.cameraController.position2d);
		}
	}
}
