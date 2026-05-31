using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown.GameData;

[Serializable]
[CreateAssetMenu(fileName = "New Lamp So", menuName = "多洛可小镇[农场]/设备灯光")]
public class LampSo : SerializedScriptableObject
{
	[SerializeField]
	protected float intensity;

	[SerializeField]
	protected Color color;

	[SerializeField]
	protected float range;

	[SerializeField]
	protected Vector2 offset;

	[SerializeField]
	protected Light2D.LightType type;

	[SerializeField]
	[ColorUsage(false, true)]
	protected Color emissionColor;

	[SerializeField]
	protected float emissionIntensity;

	[SerializeField]
	protected Sprite emissionSprite;

	public bool CreateProto(out LampProto proto)
	{
		proto = null;
		if (emissionSprite == null)
		{
			return false;
		}
		proto = new LampProto(intensity, color, range, offset, type, emissionColor, emissionIntensity, emissionSprite);
		return true;
	}

	private void CopyConfig()
	{
		HDRColorDecomposer.DecomposeHdrColor(emissionColor, out var baseLinearColor, out var exposure);
		GUIUtility.systemCopyBuffer = $"{intensity}\t" + $"{Mathf.RoundToInt(color.r * 255f)}\t{Mathf.RoundToInt(color.g * 255f)}\t{Mathf.RoundToInt(color.b * 255f)}\t{Mathf.RoundToInt(color.a * 255f)}\t" + $"{range}\t" + $"{offset.x},{offset.y}\t" + $"{type}\t" + $"{baseLinearColor.r}\t{baseLinearColor.g}\t{baseLinearColor.b}\t{exposure}\t" + $"{emissionIntensity}\t" + emissionSprite.name;
	}
}
