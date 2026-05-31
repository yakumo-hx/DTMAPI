using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Platform;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public interface IPlatformHost : IBaseHost
{
	[JsonProperty]
	PlatformManager DM_platform { get; }

	Tilemap RM_platformTilemap => DolocAPI.farmRenderer.RM_platformTilemap;

	void RenderAllPlatforms()
	{
		DolocAPI.farmRenderer.SetVisible(value: true);
		RM_platformTilemap.transform.position = Vector3.zero;
		DolocAPI.EntitySystem.SetupAll<PlatformEntityOneway, Platform>(DM_platform.totalPlatforms, RenderPlatform);
	}

	void HideAllPlatforms()
	{
		foreach (Platform totalPlatform in DM_platform.totalPlatforms)
		{
			ErasePlatformTiles(RM_platformTilemap, totalPlatform);
			DolocAPI.EntitySystem.Recycle(totalPlatform.Collider);
		}
	}

	void RenderPlatform(PlatformEntityOneway renderer, Platform data)
	{
		if (!(renderer == null) && data != null)
		{
			DrawPlatformTiles(RM_platformTilemap, data);
			RenderPlatformCollider(renderer, data);
		}
	}

	PlatformGeometry TryGetPlatformDiffProto(PlatformGeometry geometry, PlatformInfo proto)
	{
		return DM_platform.TryGetPlatformDiffProto(geometry, proto)?.geometry;
	}

	Platform CreatePlatform(PlatformInfo platformProto, PlatformGeometry geometry, PlatformCutInfos cutInfos, out List<(CountItem, Vector2)> returnCost)
	{
		returnCost = new List<(CountItem, Vector2)>();
		Platform platform = DM_platform.CreatePlatform(geometry, platformProto);
		platform.Host = this;
		foreach (KeyValuePair<Platform, List<Vector2Int>> collisionPlatform in cutInfos.CollisionPlatforms)
		{
			Platform key = collisionPlatform.Key;
			DM_terrain.RemoveContent(key);
			DM_platform.RemovePlatform(key);
			ErasePlatformTiles(RM_platformTilemap, key);
			foreach (Vector2Int item2 in collisionPlatform.Value)
			{
				int itemCount = key.geometry.CutColumn(item2);
				Vector2 item = CurrentRoom.Geometry.CalcWorldPositionCenter(item2);
				returnCost.Add((new CountItem(key.proto.Id, itemCount), item));
			}
			key = DM_platform.CreatePlatform(key.geometry, key.proto);
			DM_terrain.FillContent(key);
			key.Host = this;
			DrawPlatformTiles(RM_platformTilemap, key);
		}
		DM_terrain.FillContent(platform);
		DolocAPI.agent.StateManager.FixState(delegate
		{
			DrawPlatformTiles(RM_platformTilemap, platform);
			RefreshColliders();
		});
		return platform;
	}

	Platform CreatePlatform(PlatformInfo platformProto, PlatformGeometry geometry)
	{
		Platform platform = DM_platform.CreatePlatform(geometry, platformProto);
		platform.Host = this;
		DM_terrain.FillContent(platform);
		DolocAPI.agent.StateManager.FixState(delegate
		{
			DrawPlatformTiles(RM_platformTilemap, platform);
			RefreshColliders();
		});
		return platform;
	}

	Vector2Int TryGetValidLockedPosition(Vector2Int lockedPosition, Vector2Int currentCellPosition)
	{
		Vector2Int pos = new Vector2Int(lockedPosition.x, currentCellPosition.y);
		if (!DM_terrain.Raycast(pos, CurrentRoom.RoomGridSize.y, Vector2Int.down, TerrainLayerName.Structure, out var hitpos))
		{
			return lockedPosition;
		}
		return new Vector2Int(hitpos.x, hitpos.y + 1);
	}

	void ReplacePlatform(PlatformGeometry geometry, PlatformInfo newProto, out string oldPlatformProtoName)
	{
		oldPlatformProtoName = null;
		Platform platform = DM_platform.TryGetPlatformDiffProto(geometry, newProto);
		if (platform != null)
		{
			oldPlatformProtoName = platform.proto.Id;
			RemovePlatform(platform);
			CreatePlatform(newProto, geometry);
		}
	}

	Platform CreatePlatformFromPreset(RoomPresetObjectProto preset)
	{
		PlatformInfo orDefault = DolocConfig.Tables.TbPlatform.GetOrDefault(preset.name);
		if (orDefault == null)
		{
			Debug.LogWarning("无法找到名为\"" + preset.name + "\"的平台");
			return null;
		}
		PlatformGeometry geometry = PlatformGeometry.Calculate(preset.platformInfo.lb, preset.platformInfo.rb, preset.platformInfo.rt);
		Platform platform = DM_platform.CreatePlatform(geometry, orDefault);
		DM_terrain.FillContent(platform);
		platform.Host = this;
		return platform;
	}

	bool CanRemovePlatform(Platform platform)
	{
		return !DM_terrain.AnyFilledIgnoreTerrain(platform.geometry.SurfacePositions, TerrainLayerName.PlatformColumn);
	}

	void RemovePlatform(Platform platform)
	{
		DM_terrain.RemoveContent(platform);
		DM_platform.RemovePlatform(platform);
		ErasePlatformTiles(RM_platformTilemap, platform);
		RefreshColliders();
	}

	void DrawPlatformTiles(Tilemap renderer, Platform platform)
	{
		DrawPlatformColumnTiles(renderer, platform);
		DrawPlatformSurfaceTiles(renderer, platform);
	}

	void DrawPlatformSurfaceTiles(Tilemap renderer, Platform platform)
	{
		PlatformGeometry geometry = platform.geometry;
		Room currentRoom = CurrentRoom;
		Tile[] array = PlatformTileGeneratorUtils.GenerateSurfaceTiles(platform);
		for (int i = 0; i < array.Length; i++)
		{
			Vector2Int vector2Int = new Vector2Int(geometry.Left + i, geometry.Height);
			renderer.SetTile((Vector3Int)(vector2Int + currentRoom.RoomGridPos), array[i]);
		}
	}

	void DrawPlatformColumnTiles(Tilemap renderer, Platform platform)
	{
		PlatformGeometry geometry = platform.geometry;
		Room currentRoom = CurrentRoom;
		int num = geometry.Height - 1;
		Tile[] array = PlatformTileGeneratorUtils.GenerateColumnTiles(platform, isLeftColumn: true);
		for (int i = 0; i < array.Length; i++)
		{
			Vector2Int vector2Int = new Vector2Int(geometry.Left, num - i);
			renderer.SetTile((Vector3Int)(vector2Int + currentRoom.RoomGridPos), array[i]);
		}
		Tile[] array2 = PlatformTileGeneratorUtils.GenerateColumnTiles(platform, isLeftColumn: false);
		for (int j = 0; j < array2.Length; j++)
		{
			Vector2Int vector2Int2 = new Vector2Int(geometry.Right, num - j);
			renderer.SetTile((Vector3Int)(vector2Int2 + currentRoom.RoomGridPos), array2[j]);
		}
	}

	void ErasePlatformTiles(Tilemap renderer, Platform platform)
	{
		Room currentRoom = CurrentRoom;
		foreach (Vector2Int allPosition in platform.geometry.AllPositions)
		{
			renderer.SetTile((Vector3Int)(allPosition + currentRoom.RoomGridPos), null);
		}
	}

	void RenderPlatformCollider(PlatformEntityOneway renderer, Platform data)
	{
		data.Collider = renderer;
		renderer.Platform = data;
		Vector3Int platformInfo = data.geometry.PlatformInfo;
		Vector2 vector = CurrentRoom.RoomPosition + new Vector2(platformInfo.x, platformInfo.z + 1) * 1.5f;
		float x = (float)(platformInfo.y - platformInfo.x + 1) * 1.5f;
		renderer.transform.position = new Vector3(vector.x, vector.y, 0f);
		renderer.ptCollider.points = new Vector2[2]
		{
			Vector2.zero,
			new Vector2(x, 0f)
		};
	}

	void RefreshColliders()
	{
		DolocAPI.EntitySystem.Clear<PlatformEntityOneway>();
		foreach (Platform totalPlatform in DM_platform.totalPlatforms)
		{
			PlatformEntityOneway renderer = DolocAPI.EntitySystem.Next<PlatformEntityOneway>();
			RenderPlatformCollider(renderer, totalPlatform);
		}
		RefreshMaterialMapPt();
	}

	void RefreshMaterialMapPt();

	bool CheckPlatformValid(PlatformGeometry platformGeometry)
	{
		return CurrentRoom.DM_terrain.AllEmpty(platformGeometry.AllPositions, TerrainLayerName.Structure);
	}

	void __AfterLoadPlatforms()
	{
		if (DM_platform?.totalPlatforms == null)
		{
			return;
		}
		foreach (Platform totalPlatform in DM_platform.totalPlatforms)
		{
			totalPlatform.Host = this;
			DM_terrain.FillContent(totalPlatform);
		}
	}
}
