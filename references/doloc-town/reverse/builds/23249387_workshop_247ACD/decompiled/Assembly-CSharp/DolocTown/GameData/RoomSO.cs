using System;
using System.Linq;
using DolocTown.Config.Tile;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public class RoomSO : SerializedScriptableObject
{
	[SerializeField]
	private string roomName;

	[SerializeField]
	public RoomType roomType;

	[SerializeField]
	protected SceneInfoSO sceneInfo;

	[SerializeField]
	protected bool isInHouse;

	[SerializeField]
	protected Vector2[] inWalkableAreas;

	[SerializeField]
	private TileMaterial tileMaterial;

	[SerializeField]
	private RoomGeometrySO geometrySO;

	[SerializeField]
	protected float groundHeight;

	[SerializeField]
	protected RoomPresetObjectSO[] presetObjects;

	[SerializeField]
	protected MonsterGenInfoSO monsterGenInfo;

	[SerializeField]
	private ResourceGenInfoSO resourceInfo;

	[SerializeField]
	private ResourceGenInfoSO vegetationInfo;

	[SerializeField]
	private ResourceGenInfoSO envObjectInfo;

	[SerializeField]
	private TerrainConstraintSO envObstacles;

	[SerializeField]
	private EnvBackgroundSO background;

	[SerializeField]
	private string[] neighbourRooms;

	public string RoomName
	{
		get
		{
			if (!base.name.IsNullOrEmpty())
			{
				return base.name;
			}
			return roomName;
		}
	}

	public RoomGeometrySO Geometry => geometrySO;

	public ResourceGenInfoSO ResourceInfo => resourceInfo;

	public void ResetSO(string roomName, RoomType roomType, bool isInhouse, SceneInfoSO sceneInfo, RoomGeometrySO geometrySO, TileMaterial tileMaterial, float groundHeight, Vector2[] inWalkableAreas, RoomPresetObjectSO[] presetObjects, MonsterGenInfoSO monsterGenInfo, ResourceGenInfoSO resourceInfo, ResourceGenInfoSO vegetationInfo, ResourceGenInfoSO envObjectInfo, TerrainConstraintSO envObstacles, EnvBackgroundSO background)
	{
		this.roomName = roomName;
		this.roomType = roomType;
		isInHouse = isInhouse;
		this.sceneInfo = sceneInfo;
		this.geometrySO = geometrySO;
		this.groundHeight = groundHeight;
		this.inWalkableAreas = inWalkableAreas;
		this.presetObjects = presetObjects;
		this.monsterGenInfo = monsterGenInfo;
		this.resourceInfo = resourceInfo;
		this.vegetationInfo = vegetationInfo;
		this.envObjectInfo = envObjectInfo;
		this.envObstacles = envObstacles;
		this.background = background;
		this.tileMaterial = tileMaterial;
	}

	public void SetNeighbourRooms(string[] neighbourRooms)
	{
		this.neighbourRooms = neighbourRooms ?? Array.Empty<string>();
	}

	public bool CreateProto(out RoomProto proto)
	{
		proto = null;
		if (!geometrySO.CreateProto(out var geometry))
		{
			return false;
		}
		proto = new RoomProto(base.name.IsNullOrEmpty() ? roomName : base.name, roomType, sceneInfo.Proto, geometry, isInHouse, inWalkableAreas, groundHeight, tileMaterial, background, presetObjects.Select((RoomPresetObjectSO x) => x.Proto).ToArray(), monsterGenInfo.Proto, resourceInfo?.CreateProto() ?? ResourceGenInfoProto.Empty, vegetationInfo?.CreateProto() ?? ResourceGenInfoProto.Empty, envObjectInfo?.CreateProto() ?? ResourceGenInfoProto.Empty, envObstacles.Proto, neighbourRooms);
		return true;
	}
}
