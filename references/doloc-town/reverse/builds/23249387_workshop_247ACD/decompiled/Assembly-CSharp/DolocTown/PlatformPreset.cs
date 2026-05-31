using System;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

[RequireComponent(typeof(TilemapRenderer), typeof(Tilemap))]
public class PlatformPreset : ObjectPreset
{
	[SerializeField]
	protected string platformTitle;

	[SerializeField]
	[HideInInspector]
	protected Vector2Int gridSize;

	public PlatformPositionInfo platformInfo;

	public override Sprite Sprite => null;

	public override Vector2Int GridSize => gridSize;

	public override string PresetObjectName => platformTitle;

	public override Vector2Int LocalGridPosition
	{
		get
		{
			RoomHandle componentInParent = GetComponentInParent<RoomHandle>();
			if (componentInParent != null)
			{
				return GlobalGridPosition - componentInParent.scenePos.LatticeToGrid(1.5f);
			}
			return GlobalGridPosition;
		}
	}

	public override Vector2Int GlobalGridPosition
	{
		get
		{
			Vector2Int vector2Int = new Vector2Int(Math.Min(platformInfo.lb.x, platformInfo.rb.x), Math.Min(platformInfo.lb.y, platformInfo.rb.y));
			RoomHandle componentInParent = GetComponentInParent<RoomHandle>();
			if (componentInParent != null)
			{
				return vector2Int + componentInParent.scenePos.LatticeToGrid(1.5f);
			}
			return vector2Int;
		}
	}

	private void OnPlatformNameChanged()
	{
		base.gameObject.name = platformTitle ?? "未知平台";
		RoomHandle componentInParent = GetComponentInParent<RoomHandle>();
		if (componentInParent != null)
		{
			base.transform.position = componentInParent.roomPosition;
		}
	}

	private void OnPlatformPosChanged()
	{
		platformInfo.rt.x = platformInfo.rb.x;
		if (platformInfo.rb.y >= platformInfo.rt.y)
		{
			gridSize = Vector2Int.zero;
			Debug.LogError("该平台数据不合法");
			return;
		}
		PlatformGeometry platformGeometry = PlatformGeometry.Calculate(platformInfo.lb, platformInfo.rb, platformInfo.rt);
		Vector2Int builderPlatformWidth = DolocAPI.GlobalParameter.BuilderPlatformWidth;
		if (platformGeometry.Width > builderPlatformWidth.y || platformGeometry.Width < builderPlatformWidth.x)
		{
			Debug.LogError("支架宽度无效");
		}
	}

	public override bool CreatePreset(out RoomPresetObjectSO preset)
	{
		if (gridSize == Vector2Int.zero)
		{
			preset = null;
			return false;
		}
		string presetObjectName = PresetObjectName;
		Vector2Int localGridPosition = LocalGridPosition;
		PlatformPositionInfo platformPositionInfo = platformInfo;
		preset = new RoomPresetObjectSO(RoomPresetObjectType.Platform, presetObjectName, localGridPosition, 0, disableRefresh: false, default(CountItemListConfig), platformPositionInfo);
		return true;
	}
}
