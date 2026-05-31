using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOShakeScreen : EffectsSO
{
	[SerializeField]
	[Range(0.1f, 2f)]
	private float shakeDuration = 0.2f;

	[SerializeField]
	[Range(0.1f, 2f)]
	private float shakeStrength = 0.3f;

	public override void Raise(Vector2 posWS)
	{
		DolocAPI.cameraController.ShakeScreen(shakeDuration, shakeStrength);
	}

	public override void Raise(Vector2 posWS, Vector2 dir)
	{
		DolocAPI.cameraController.ShakeScreen(shakeDuration, shakeStrength);
	}
}
