using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class Case : Equipment, IContainer, ILocatable
{
	public EquipmentFuncCase func;

	private readonly GameEntitySlotUI<CaseAutomateLabelTip> labelTip = new GameEntitySlotUI<CaseAutomateLabelTip>();

	public override string Title => title;

	[JsonProperty]
	public LinearInventory inventory { get; protected set; }

	[JsonProperty]
	public int skinIndex { get; protected set; }

	[JsonProperty]
	public string customCaseName { get; protected set; }

	[JsonProperty]
	[DebugInfo("标签", AllowEdit = true)]
	public HashSet<string> labels { get; private set; }

	[JsonProperty]
	[DebugInfo("输出标记", AllowEdit = true)]
	public bool isOutput { get; private set; }

	[JsonProperty]
	[DebugInfo("输入标记", AllowEdit = true)]
	public bool isInput { get; private set; }

	public string title
	{
		get
		{
			if (!string.IsNullOrEmpty(customCaseName))
			{
				return customCaseName;
			}
			return proto.Title;
		}
		set
		{
			customCaseName = ((value == proto.Title) ? "" : value);
		}
	}

	public int totalCapacity => func.TotalCapacity;

	public int lineCapacity => func.LineCapacity;

	public override bool IsDirty
	{
		get
		{
			if (inventory.filledCount <= 0)
			{
				return !customCaseName.IsNullOrEmpty();
			}
			return true;
		}
	}

	[DebugInfo("共享库存")]
	public bool IsShared { get; set; }

	public override string ExtraInfoAsItem
	{
		get
		{
			if (func.PlaceCondition_Ref != null)
			{
				List<string> list = new List<string>();
				foreach (ItemMainTypeInfo item in func.PlaceCondition_Ref.ItemTypeFilter_Ref)
				{
					list.Add(item.Title);
				}
				foreach (ItemSubTypeInfo item2 in func.PlaceCondition_Ref.ItemSubTypeFilter_Ref)
				{
					list.Add(item2.Title);
				}
				foreach (ItemInfo item3 in func.PlaceCondition_Ref.ItemNameFilter_Ref)
				{
					list.Add(DolocAPI.GetItemTitle(item3.Title));
				}
				return DolocUtils.Format(DolocConfig.StaticTexts.ItemContainerConditionFormat, list.HandleJoinString());
			}
			return string.Empty;
		}
	}

	public Case(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncCase)proto.Function;
		inventory = new LinearInventory(totalCapacity);
		labels = new HashSet<string>(DolocConfig.Tables.TbItemAutomationType.DataList.Select((ItemAutomationTypeInfo x) => x.Id));
		isOutput = false;
		isInput = false;
	}

	[JsonConstructor]
	protected Case(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, LinearInventory inventory, int skinIndex, string customCaseName, HashSet<string> labels, bool isOutput, bool isInput)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		func = (EquipmentFuncCase)proto.Function;
		this.inventory = inventory ?? new LinearInventory(totalCapacity);
		this.skinIndex = skinIndex;
		this.customCaseName = customCaseName;
		if (labels == null)
		{
			labels = new HashSet<string>(DolocConfig.Tables.TbItemAutomationType.DataList.Select((ItemAutomationTypeInfo x) => x.Id));
		}
		this.labels = labels;
		this.isOutput = isOutput;
		this.isInput = isInput;
		this.inventory.ValidateCapacity(totalCapacity);
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		HashSet<string> other = new HashSet<string>(DolocConfig.Tables.TbItemAutomationType.DataList.Select((ItemAutomationTypeInfo x) => x.Id));
		labels.IntersectWith(other);
	}

	public bool ContentFilter(Item item)
	{
		if (func.PlaceCondition_Ref != null && !func.PlaceCondition_Ref.CheckCondition(item))
		{
			return false;
		}
		return DolocAPI.IsItemCanPutInToContainer(item);
	}

	public bool IsAutomateLabelMatch(Item item)
	{
		return labels.Contains(item.automationType.Id);
	}

	public bool IsAutomateLabelMatch(string automateTypeId)
	{
		return labels.Contains(automateTypeId);
	}

	public bool IsAutomateLabelMatch(DropItemBase dropItem)
	{
		if (!(dropItem is SpecialDropItem specialDropItem))
		{
			if (dropItem is DropItem dropItem2)
			{
				ItemInfo itemInfo;
				return DolocAPI.QueryItemProto(dropItem2.ItemName, out itemInfo) && labels.Contains(itemInfo.SubType_Ref.AutomationType_Ref.Id);
			}
			return false;
		}
		return IsAutomateLabelMatch(specialDropItem.DropItem);
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		DolocAPI.EnterUI((FarmCaseUiState state) => state.HandleStartUpArgs(this, delegate
		{
			ChangeTipPrompt(title);
			RefreshSprite();
		}, ExtraInfoAsItem));
		SendUseEquipmentMessage();
		DolocAPI.Broadcast(OperationEventType.USE_CASE_EQUIPMENT);
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(title);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void OnRender()
	{
		base.OnRender();
		RefreshSprite();
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
	}

	protected override void OnRemove()
	{
		base.OnRemove();
		IsShared = false;
	}

	public void SetSkinIndex(int index)
	{
		skinIndex = Mathf.Clamp(index, 0, func.SkinCount - 1);
	}

	private void RefreshSprite()
	{
		if (base.IsRender)
		{
			Sprite skinSprite = func.GetSkinSprite(skinIndex);
			if (skinSprite != null)
			{
				base.Renderer.Sprite = skinSprite;
			}
		}
	}
}
