using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class Battery : Equipment
{
	private readonly ElectronicComponentBattery battery;

	private readonly bool hideProgressBar;

	private int lastSpriteIndex = -1;

	private EquipmentFuncBattery func => (EquipmentFuncBattery)proto.Function;

	public override Sprite EquipmentSprite => GetSpriteByCurrentPower();

	public override bool IsDirty => battery.Power > 0f;

	private Vector3 BarPosition
	{
		get
		{
			Vector3 positionBottom = base.PositionBottom;
			positionBottom.z = -6f;
			return positionBottom;
		}
	}

	public Battery(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		battery = (ElectronicComponentBattery)electronicComponent;
		hideProgressBar = !func.Appearance.assets.IsNullOrEmpty() && func.Appearance.assets[0] != null;
	}

	[JsonConstructor]
	protected Battery(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		battery = (ElectronicComponentBattery)electronicComponent;
		hideProgressBar = !func.Appearance.assets.IsNullOrEmpty() && func.Appearance.assets[0] != null;
	}

	private int GetSpriteIndexByCurrentPower()
	{
		return Mathf.Clamp(Mathf.RoundToInt(battery.PowerPercent * (float)func.Appearance.assets.Length), 0, func.Appearance.assets.Length - 1);
	}

	private void RefreshSprite()
	{
		if (base.IsRender && !(base.Renderer == null))
		{
			int spriteIndexByCurrentPower = GetSpriteIndexByCurrentPower();
			if (lastSpriteIndex != spriteIndexByCurrentPower)
			{
				lastSpriteIndex = spriteIndexByCurrentPower;
				Sprite sprite = func.Appearance.assets[lastSpriteIndex] ?? GetDefaultSprite();
				base.Renderer.Sprite = sprite;
			}
		}
	}

	private Sprite GetDefaultSprite()
	{
		if (!base.Turn)
		{
			return proto.Sprite;
		}
		return proto.TurnSprite;
	}

	private Sprite GetSpriteByCurrentPower()
	{
		if (!hideProgressBar)
		{
			return GetDefaultSprite();
		}
		int spriteIndexByCurrentPower = GetSpriteIndexByCurrentPower();
		return func.Appearance.assets[spriteIndexByCurrentPower] ?? GetDefaultSprite();
	}

	protected override void Update()
	{
		if (hideProgressBar)
		{
			RefreshSprite();
		}
		else if (base.IsTouch && !hideProgressBar)
		{
			base.Renderer.UpdateProgressBarRenderer(battery.PowerPercent, BarPosition);
		}
	}

	protected override void OnTouch()
	{
		if (base.IsTouch && !hideProgressBar)
		{
			base.Renderer.UpdateProgressBarRenderer(battery.PowerPercent, BarPosition);
		}
	}

	protected override void OnDisTouch()
	{
		base.Renderer.RemoveProgressBarRenderer();
	}

	public override void OnCreated()
	{
		base.OnCreated();
		if (hideProgressBar)
		{
			lastSpriteIndex = -1;
			RefreshSprite();
		}
		else
		{
			base.Renderer.Sprite = GetDefaultSprite();
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		if (hideProgressBar)
		{
			lastSpriteIndex = -1;
			RefreshSprite();
		}
		else
		{
			base.Renderer.Sprite = GetDefaultSprite();
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.RemoveProgressBarRenderer();
	}
}
