using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.TechTree;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FishIncubator : EquipmentWorker, IContainer
{
	private readonly EquipmentFuncFishIncubator func;

	private readonly Queue<ItemFishRoe> queueToFry = new Queue<ItemFishRoe>();

	private readonly Counter tuCounter;

	private bool _hasFishRoe;

	public override bool IsDirty => !inventory.isEmpty;

	[JsonProperty]
	public LinearInventory inventory { get; private set; }

	public string title => proto.Title;

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public FishIncubator(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncFishIncubator)proto.Function;
		inventory = new LinearInventory(func.TotalCapacity);
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
	}

	[JsonConstructor]
	public FishIncubator(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, LinearInventory inventory, bool isIdle, bool isWorking, Counter counter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		if (proto != null)
		{
			func = (EquipmentFuncFishIncubator)proto.Function;
			this.inventory = inventory ?? new LinearInventory(func.TotalCapacity);
			tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
			ResolveStatus();
		}
	}

	private void ResolveStatus()
	{
		if (inventory.isEmpty)
		{
			_hasFishRoe = false;
			return;
		}
		_hasFishRoe = false;
		Item[] array = inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemFishRoe)
			{
				_hasFishRoe = true;
				break;
			}
		}
	}

	public override void OnWorking(bool isRender)
	{
		if (Incubate())
		{
			ForceStopCurrentWork();
		}
		if (isRender)
		{
			UpdateRenderer();
		}
	}

	private bool Incubate()
	{
		if (!tuCounter.Tick())
		{
			return false;
		}
		if (inventory.isEmpty)
		{
			return true;
		}
		queueToFry.Clear();
		int num = 0;
		Item[] array = inventory.ReadAll();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is ItemFishRoe itemFishRoe)
			{
				if (itemFishRoe.Incubate())
				{
					queueToFry.Enqueue(itemFishRoe);
				}
				else
				{
					num++;
				}
			}
		}
		while (queueToFry.Count > 0)
		{
			ItemFishRoe itemFishRoe2 = queueToFry.Dequeue();
			int num2 = inventory.IndexOf(itemFishRoe2);
			inventory.SwapItem(num2, itemFishRoe2.ToFry());
			DolocAPI.BroadcastString(GameEventType.AQUA_FISH_GROW_UP, itemFishRoe2.fishName);
			DolocAPI.AddTechExp(TechPointType.ANIMAL, DolocConfig.Tables.TbFarmFish.GetTechPoint(itemFishRoe2.fishName));
		}
		_hasFishRoe = num > 0;
		return num == 0;
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationView);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		DolocAPI.EnterUI((FishTankUiState state) => state.HandleContainerStartUpArgs(this, _OnCloseInventory, () => ""));
	}

	protected override void OnRender()
	{
		UpdateRenderer();
	}

	private void UpdateRenderer()
	{
		if (base.IsRender)
		{
			base.Renderer.Sr.RenderAsFishIncubator(func.MaskSprite.Asset, func.EmissionSprite.Asset, base.IsWorking);
		}
	}

	protected override void OnUnRender()
	{
		base.Renderer.Sr.RenderAsNormal();
	}

	private void _OnCloseInventory()
	{
		ResolveStatus();
		if (!_hasFishRoe)
		{
			if (base.IsWorking)
			{
				ForceStopCurrentWork();
			}
			UpdateRenderer();
			return;
		}
		int n = (from x in inventory.ReadAll()
			where x is ItemFishRoe
			select ((ItemFishRoe)x).IncubateGap).Max();
		ForceWork(n);
	}

	public bool ContentFilter(Item content)
	{
		if (!(content is ItemFishFry))
		{
			return content is ItemFishRoe;
		}
		return true;
	}
}
