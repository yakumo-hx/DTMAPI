using System;
using DolocTown.Config.Tile;
using UnityEngine;

namespace DolocTown.GameData;

public class RoomProto
{
	public readonly string name;

	public readonly RoomType roomType;

	public readonly SceneInfo sceneInfo;

	public readonly RoomGeometry geometry;

	public readonly bool isInHouse;

	public readonly Vector2[] inWalkableAreas;

	public readonly float groundHeight;

	public readonly TileMaterial defaultTileMaterial;

	public readonly EnvBackgroundSO background;

	public readonly RoomPresetObjectProto[] presetObjects;

	public readonly MonsterGenInfoProto monsterInfo;

	public readonly ResourceGenInfoProto resourceInfo;

	public readonly ResourceGenInfoProto vegetationInfo;

	public readonly ResourceGenInfoProto envObjectGenInfo;

	public readonly TerrainConstraintProto envObstacles;

	public readonly string[] neighbourRooms;

	public float GroundLine
	{
		get
		{
			if (!isInHouse)
			{
				return groundHeight;
			}
			return geometry.roomPosition.y;
		}
	}

	public Vector2 RandomStreetPoint
	{
		get
		{
			if (isInHouse)
			{
				Vector2 roomPosition = geometry.roomPosition;
				Vector2 roomSize = geometry.roomSize;
				return new Vector2(UnityEngine.Random.Range(roomPosition.x, roomPosition.x + roomSize.x), roomPosition.y);
			}
			Vector2 scenePosition = geometry.scenePosition;
			Vector2 sceneSize = geometry.sceneSize;
			return new Vector2(UnityEngine.Random.Range(scenePosition.x, scenePosition.x + sceneSize.x), groundHeight);
		}
	}

	public RoomProto(string name, RoomType roomType, SceneInfo sceneInfo, RoomGeometry geometry, bool isInHouse, Vector2[] inWalkableAreas, float groundHeight, TileMaterial defaultTileMaterial, EnvBackgroundSO background, RoomPresetObjectProto[] presetObjects, MonsterGenInfoProto monsterInfo, ResourceGenInfoProto resourceInfo, ResourceGenInfoProto vegetationInfo, ResourceGenInfoProto envObjectGenInfo, TerrainConstraintProto envObstacles, string[] neighbourRooms = null)
	{
		this.name = name;
		this.roomType = roomType;
		this.sceneInfo = sceneInfo;
		this.geometry = geometry;
		this.isInHouse = isInHouse;
		this.inWalkableAreas = inWalkableAreas;
		this.groundHeight = groundHeight;
		this.defaultTileMaterial = defaultTileMaterial;
		this.background = background;
		this.presetObjects = presetObjects;
		this.monsterInfo = monsterInfo;
		this.resourceInfo = resourceInfo;
		this.vegetationInfo = vegetationInfo;
		this.envObjectGenInfo = envObjectGenInfo;
		this.envObstacles = envObstacles;
		this.neighbourRooms = neighbourRooms ?? Array.Empty<string>();
	}
}
