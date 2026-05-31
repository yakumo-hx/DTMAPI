using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown.GameData;

public class LampProto
{
	public float Intensity { get; private set; }

	public Color Color { get; private set; }

	public float Range { get; private set; }

	public Vector2 Offset { get; private set; }

	public Light2D.LightType Type { get; private set; }

	public Color EmissionColor { get; private set; }

	public float EmissionIntensity { get; private set; }

	public Sprite EmissionSprite { get; private set; }

	public LampProto(float intensity, Color color, float range, Vector2 offset, Light2D.LightType type, Color emissionColor, float emissionIntensity, Sprite emissionSprite)
	{
		Intensity = intensity;
		Color = color;
		Range = range;
		Offset = offset;
		Type = type;
		EmissionColor = emissionColor;
		EmissionIntensity = emissionIntensity;
		EmissionSprite = emissionSprite;
	}
}
