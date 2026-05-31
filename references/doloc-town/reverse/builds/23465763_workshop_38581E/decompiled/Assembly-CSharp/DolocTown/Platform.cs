using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Platform;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class Platform : TerrainContent, IDismantleable, IComparable<Platform>
{
	private int knockCount;

	private Coroutine coroutine;

	private TerrainTilemapRenderer builderRenderer;

	public readonly PlatformInfo proto;

	[JsonProperty]
	public readonly PlatformGeometry geometry;

	public int MaxKnockCount => 3;

	public List<Equipment> OccupyEquipments
	{
		get
		{
			List<Equipment> list = new List<Equipment>();
			Vector2Int[] array = geometry.SurfacePositions.ToArray();
			foreach (Equipment contentsFromPosition in CurrentRoom.DM_terrain.GetContentsFromPositions<Equipment>(array))
			{
				if (!contentsFromPosition.proto.isDecal && (array.Contains(contentsFromPosition.Anchor) || array.Contains(contentsFromPosition.RightBottom)) && !CurrentRoom.DM_terrain.AllFilled(contentsFromPosition.proto.GroundPositions(contentsFromPosition.Anchor), TerrainLayerName.CeilingFront))
				{
					list.Add(contentsFromPosition);
				}
			}
			return list;
		}
	}

	private IEnumerable<Platform> OccupyPlatforms
	{
		get
		{
			HashSet<Platform> hashSet = new HashSet<Platform>();
			foreach (Vector2Int surfacePosition in geometry.SurfacePositions)
			{
				if (CurrentRoom.DM_terrain.QueryContent<Platform>(surfacePosition, TerrainLayerName.PlatformColumn, out var cnt))
				{
					hashSet.Add(cnt);
				}
			}
			return hashSet;
		}
	}

	public IPlatformHost Host { get; set; }

	public Room CurrentRoom => Host.CurrentRoom;

	[JsonProperty]
	private string name => proto.Id;

	[JsonProperty]
	public int randomSeed { get; private set; }

	public PlatformEntityOneway Collider { get; set; }

	public int Height => geometry.Height;

	public void OnFell(Vector2 hitPosition)
	{
		DolocAPI.RaiseInstantAnimEffects(hitPosition, InstAnimEffectType.HIT_SPARK, LocMaterials.GAME_MAT_EFFECTS_SHINE);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_KNOCKING_EQUIPMENT);
		if (!Host.CanRemovePlatform(this))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrPlatformOccupied);
			return;
		}
		knockCount++;
		if (knockCount < MaxKnockCount)
		{
			if (knockCount == 1)
			{
				int num = 0;
				foreach (Equipment occupyEquipment in OccupyEquipments)
				{
					if (occupyEquipment.IsOccupy)
					{
						num++;
					}
					else
					{
						occupyEquipment.Renderer.Shake();
					}
				}
				if (num > 0)
				{
					knockCount = 0;
					DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.FarmbuilderErrPlatformRemove);
					return;
				}
				builderRenderer = BuilderUtils.CreatePlatformIndicator(geometry, proto, CurrentRoom.RoomPosition);
				builderRenderer.SetColor(DolocAPI.eftConfig.validIndicatorColor);
			}
			else
			{
				foreach (Equipment occupyEquipment2 in OccupyEquipments)
				{
					occupyEquipment2.WaitRemove = true;
					occupyEquipment2.Renderer.Shiner(2f);
					occupyEquipment2.Renderer.Shake(2f);
				}
				builderRenderer.SetColor(DolocAPI.eftConfig.invalidIndicatorColor);
				builderRenderer.Shake();
			}
			DolocAPI.StopCoroutine(coroutine);
			coroutine = DolocAPI.Delay(2f, OnRecover);
			return;
		}
		DolocAPI.StopCoroutine(coroutine);
		OnRecover();
		Host.RemovePlatform(this);
		Vector2 pos = CurrentRoom.Geometry.CalcWorldPosition(geometry.CenterTop);
		DolocAPI.GenerateDropItems(CurrentRoom, proto.Id, pos, geometry.TileCount, shouldSendMsg: false);
		foreach (Equipment occupyEquipment3 in OccupyEquipments)
		{
			((IEquipmentHost)Host).RemoveEquipment(occupyEquipment3);
		}
		DolocAPI.RefreshScanner();
		Host.CurrentRoom.OnPlatformChanged();
		DolocAPI.Broadcast(OperationEventType.FELL_PLATFORM);
		DolocAPI.effectProvider.RaiseInstPS(hitPosition, InstantParticleEffectsType.SAWDUST);
	}

	public void OnRecover()
	{
		foreach (Equipment occupyEquipment in OccupyEquipments)
		{
			occupyEquipment.OnRecover();
		}
		knockCount = 0;
		DolocAPI.EntitySystem.Recycle(builderRenderer);
		builderRenderer = null;
		coroutine = null;
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]>
		{
			{
				TerrainLayerName.PlatformSurface,
				geometry.GetFillPositions()
			},
			{
				TerrainLayerName.PlatformColumn,
				geometry.GetColumnPositions().ToArray()
			}
		};
	}

	public Platform(int id, PlatformInfo proto, PlatformGeometry geometry)
		: base(id, geometry.Anchor, Vector3.zero)
	{
		this.proto = proto;
		this.geometry = geometry;
		randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
	}

	[JsonConstructor]
	private Platform(int id, string name, PlatformGeometry geometry, int randomSeed)
		: base(id, geometry.Anchor, Vector3.zero)
	{
		if (!DolocAPI.QueryPlatformProto(name, out proto))
		{
			if (DolocConfig.Tables.TbPlatform.DataList.Count == 0)
			{
				return;
			}
			proto = DolocConfig.Tables.TbPlatform.DataList.First();
		}
		this.geometry = geometry;
		this.randomSeed = ((randomSeed == 0) ? UnityEngine.Random.Range(1, int.MaxValue) : randomSeed);
	}

	protected override bool ValidateDeserialization()
	{
		return proto != null;
	}

	public override void ShiftTerrainContent(Vector2Int offset, Vector3 positionOffset)
	{
		base.ShiftTerrainContent(offset, positionOffset);
		geometry.Shift(offset);
	}

	public int CompareTo(Platform other)
	{
		return geometry.Left - other.geometry.Left;
	}
}
