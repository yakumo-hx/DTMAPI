using DolocTown.Config;
using DolocTown.Config.Platform;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class PlatformBuilderHelper
{
	private readonly PlatformBuilderRenderer _renderer;

	private readonly PlatformInfo _platformProto;

	private readonly Room _room;

	public Vector2Int LockedPosition;

	public PlatformGeometry platformGeometry { get; private set; }

	public PlatformCutInfos platformCutInfos { get; private set; }

	public bool isReplace { get; private set; }

	public bool canBuildNow { get; private set; }

	public int validHeight { get; private set; }

	private IPlatformHost PlatformHost => _room;

	private Vector2Int builderPlatformWidth => DolocAPI.GlobalParameter.BuilderPlatformWidth;

	public PlatformBuilderHelper(Room room, PlatformBuilderRenderer renderer, PlatformInfo platformProto)
	{
		_room = room;
		_renderer = renderer;
		_platformProto = platformProto;
		validHeight = room.GetRoomValidHeight();
	}

	public void RaycastGround(Vector2Int pos)
	{
		Vector2Int lB = PlatformHost.TryGetValidLockedPosition(LockedPosition, pos);
		Vector2Int vector2Int = new Vector2Int(pos.x, Mathf.Max(lB.y, pos.y));
		Vector2Int hitpos;
		bool hitGround = _room.DM_terrain.Raycast(vector2Int, _room.RoomGridSize.y, Vector2Int.down, TerrainLayerName.Structure, out hitpos);
		hitpos.y++;
		this.platformGeometry = PlatformGeometry.Calculate(lB, hitpos, vector2Int);
		isReplace = false;
		PlatformGeometry platformGeometry = PlatformHost.TryGetPlatformDiffProto(this.platformGeometry, _platformProto);
		if (platformGeometry == null)
		{
			BuildConfigure_Normal(hitGround);
			return;
		}
		isReplace = true;
		this.platformGeometry = platformGeometry;
		BuildConfigure_Replace();
	}

	public bool IsPlatformConstructable()
	{
		if (platformGeometry.Width > builderPlatformWidth.y || platformGeometry.Width < builderPlatformWidth.x)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrPlatformWidth);
			return false;
		}
		if (platformGeometry.Height + 1 > validHeight - DolocAPI.GlobalParameter.BuilderPlatformHeightLimit)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrPlatformHeightMax);
			return false;
		}
		if (!isReplace && !PlatformHost.CheckPlatformValid(platformGeometry))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrInvalidPosition);
			return false;
		}
		if (!CanAffordCost())
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrLackOfAsset);
			return false;
		}
		return true;
	}

	private void BuildConfigure_Normal(bool hitGround)
	{
		platformCutInfos = PlatformHost.DM_platform.CalculateCutInfos(platformGeometry);
		if (hitGround)
		{
			bool flag = platformGeometry.Width >= builderPlatformWidth.x && platformGeometry.Width <= builderPlatformWidth.y;
			bool flag2 = platformGeometry.Height + 1 <= validHeight - DolocAPI.GlobalParameter.BuilderPlatformHeightLimit;
			canBuildNow = flag && flag2 && CanAffordCost() && PlatformHost.CheckPlatformValid(platformGeometry);
			int num = Mathf.Max(platformGeometry.columnHeight.x, platformGeometry.columnHeight.y);
			UpdateCostInfos(platformGeometry.TileCount);
			_renderer.SetDraft(platformGeometry, _platformProto, BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, platformGeometry.Height + 1), _room.RoomPosition));
			_renderer.SetCutInfos(platformCutInfos.AllCutPositions, DolocAPI.eftConfig.cutTile);
			int y = platformGeometry.Height - num;
			_renderer.AreaPosition = BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, y), _room.RoomPosition);
		}
		else
		{
			canBuildNow = false;
			_renderer.SetDraft(platformGeometry, _platformProto);
			_renderer.SetCutInfos(platformCutInfos.AllCutPositions, DolocAPI.eftConfig.cutTile);
		}
		_renderer.DraftValid = canBuildNow;
	}

	private void BuildConfigure_Replace()
	{
		canBuildNow = true;
		int num = Mathf.Max(platformGeometry.columnHeight.x, platformGeometry.columnHeight.y);
		_renderer.SetDraft(platformGeometry, _platformProto, BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, platformGeometry.Height + 1), _room.RoomPosition));
		int y = platformGeometry.Height - num;
		_renderer.AreaPosition = BuilderUtils.GetScreenLatticePosition(new Vector2Int(platformGeometry.Left, y), _room.RoomPosition);
		_renderer.DraftValid = canBuildNow;
	}

	private void UpdateCostInfos(int count)
	{
		CostViewerData data = new CostViewerData(new CountItem[1]
		{
			new CountItem(_platformProto.Id, count)
		}, DolocAPI.archiveHandle.InventorySystem.inventory, 0, useCostInfo: false);
		_renderer.UpdateCostInfos(data);
	}

	private bool CanAffordCost()
	{
		return platformGeometry.TileCount <= DolocAPI.CountItem(_platformProto.Id);
	}
}
