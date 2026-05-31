using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Resource;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class Vegetation : TerrainContent
{
	public readonly VegetationInfo proto;

	[JsonProperty]
	public string VegetationName => proto.Id;

	protected virtual bool generateDrop => true;

	public bool isRender => Renderer != null;

	public IVegetationHost Host { get; set; }

	public VegetationRenderer Renderer { get; set; }

	public abstract Sprite CurrentSprite { get; }

	public bool isValid => proto != null;

	public virtual bool OnlyTouch => true;

	public bool CanInteractContinues => false;

	public virtual bool DisableAfterInteract => false;

	public Vector2 PositionTip
	{
		get
		{
			float b = CurrentSprite.rect.size.y * 0.125f + 1.5f;
			b = Mathf.Max(4.5f, b);
			Vector2 position2d = Renderer.position2d;
			position2d.y += b;
			return position2d;
		}
	}

	protected override Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor)
	{
		return new Dictionary<TerrainLayerName, Vector2Int[]> { 
		{
			TerrainLayerName.Vegetation,
			Grid2D.CoverHArray(anchor, proto.Width)
		} };
	}

	public Vegetation(VegetationInfo proto, Vector2Int anchor, Vector3 position, Vector2Int[] cvPositions)
		: base(-1, anchor, position)
	{
		this.proto = proto;
	}

	[JsonConstructor]
	protected Vegetation(int index, Vector2Int anchor, Vector3 position, string vegetationName)
		: base(index, anchor, position)
	{
		DolocAPI.QueryVegetationProto(vegetationName, out proto);
	}

	protected override bool ValidateDeserialization()
	{
		return proto != null;
	}

	public virtual void UpdatePerTu()
	{
	}

	public virtual void UpdatePerTuNoRender()
	{
	}

	public virtual void OnRender()
	{
	}

	public virtual void OnUnRender()
	{
	}

	public virtual void RandomInitState()
	{
	}

	public virtual void OnTouch()
	{
		Renderer.SwingOnTouch(light: true);
	}

	public virtual void OnDisTouch()
	{
	}

	public virtual void OnInteract()
	{
	}

	protected void ShowTip(string label, string key)
	{
		this.ShowSceneOperationTip(PositionTip, label, key);
	}

	protected void HideTip()
	{
		this.HideSceneOperationTip();
	}

	protected void PushTip()
	{
		this.PushSceneOperationTip();
	}

	protected void PushTipToDisappear()
	{
		this.PushSceneOperationTipToDisappear();
	}

	public virtual bool OnAttack(float attack, bool isCritical, Vector2 pos)
	{
		return false;
	}

	public virtual bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		if (!CheckToolConstraints(tool))
		{
			return false;
		}
		if (generateDrop)
		{
			GenerateDropItems();
		}
		Host.RemoveVegetation(this);
		return true;
	}

	public virtual void OnMonsterTouch(Vector2 pos)
	{
		Renderer.SwingOnTouch(light: true);
	}

	public virtual void OnMonsterDisTouch(Vector2 pos)
	{
	}

	public virtual void OnWindBlow(Vector2 pos)
	{
		Renderer.SwingOnBlow();
	}

	public virtual void OnBomb(float damage, bool ctr, Vector2 pos)
	{
		if (generateDrop)
		{
			GenerateDropItems();
		}
		Host.RemoveVegetation(this);
	}

	protected bool CheckToolConstraints(ItemTool tool)
	{
		if (!proto.GetTargetToolLevelByType(tool.ToolType, out var minLevel))
		{
			return false;
		}
		if (tool.Level < minLevel)
		{
			return false;
		}
		return true;
	}

	protected bool CheckGrowingMonth()
	{
		if (!proto.GrowingMonths.IsNullOrEmpty())
		{
			return proto.GrowingMonths.Contains(DolocAPI.archiveHandle.DateNow.Month);
		}
		return true;
	}

	protected void GenerateDropItems()
	{
		DolocAPI.GenerateDropItems((IDropItemHost)Host, proto.DropSpawnEntry, Renderer.position2d);
	}

	protected void RaiseInstantPSEffects(string effect, Vector2 pos)
	{
		if (Enum.TryParse<InstantParticleEffectsType>(effect, ignoreCase: true, out var result))
		{
			DolocAPI.RaiseInstantPSEffects(pos, result);
		}
	}

	protected void SendGatherMessage()
	{
		DolocAPI.BroadcastString(GameEventType.GATHERING_VEGETATION_FRUIT, proto.Id);
		DolocAPI.archiveHandle.RecordCollection(CollectionType.Resource, proto.Id);
	}
}
