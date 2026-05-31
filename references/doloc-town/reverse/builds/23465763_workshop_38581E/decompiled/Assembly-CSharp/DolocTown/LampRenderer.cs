using DolocTown.Config.Equipment;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

[GameEntityManager("/farm/lamp", DolocGameAssets.GAME_ENTITY_LAMP)]
[RequireComponent(typeof(Light2D))]
public class LampRenderer : GameEntity
{
	private Light2D entity;

	private float intensityScale;

	private float intensityValue;

	public float IntensityScale
	{
		get
		{
			return intensityScale;
		}
		set
		{
			intensityScale = value;
			entity.intensity = intensityValue * value;
		}
	}

	public float Intensity
	{
		get
		{
			return intensityValue;
		}
		set
		{
			intensityValue = value;
			entity.intensity = value;
		}
	}

	public Color Color
	{
		get
		{
			return entity.color;
		}
		set
		{
			entity.color = value;
		}
	}

	public float Range
	{
		get
		{
			return entity.pointLightOuterRadius;
		}
		set
		{
			entity.pointLightOuterRadius = value;
		}
	}

	public Vector2 Offset
	{
		get
		{
			return base.transform.localPosition;
		}
		set
		{
			base.transform.localPosition = value;
		}
	}

	public Light2D.LightType Type
	{
		get
		{
			return entity.lightType;
		}
		set
		{
			entity.lightType = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		entity = GetComponent<Light2D>();
	}

	public void Render(LampInfo lamp, Vector2 position)
	{
		SetVisible(value: true);
		Intensity = lamp.Intensity;
		Color = lamp.Color;
		Range = lamp.Range;
		Offset = lamp.Offset + position;
		Type = lamp.LampType;
	}
}
