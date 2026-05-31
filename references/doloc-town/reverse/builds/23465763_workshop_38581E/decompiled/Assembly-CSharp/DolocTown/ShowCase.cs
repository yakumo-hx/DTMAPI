using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class ShowCase : Equipment
{
	[JsonProperty("item")]
	private Item currentItem;

	private readonly Counter particleCounter = new Counter(10);

	private GameEntitySlot<SingleSpriteRender> itemSlot = new GameEntitySlot<SingleSpriteRender>();

	private GameEntitySlot<SingleSpriteRender> foregroundSlot = new GameEntitySlot<SingleSpriteRender>();

	private ContinuesParticleEffects particleEffects;

	private EquipmentFuncShowCase func => proto.Function as EquipmentFuncShowCase;

	public override bool IsOccupy
	{
		get
		{
			if (currentItem != null)
			{
				return !DolocAPI.IsItemDisposable(currentItem);
			}
			return false;
		}
	}

	private string CurrentPrompt
	{
		get
		{
			if (currentItem != null)
			{
				return DolocConfig.StaticTexts.UiOperationTakeoff;
			}
			return DolocConfig.StaticTexts.UiOperationDisplay;
		}
	}

	public ShowCase(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
	}

	[JsonConstructor]
	public ShowCase(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Item currentItem)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			this.currentItem = currentItem?.CheckValid();
		}
	}

	private void RefreshDisplayedItem()
	{
		if (!base.IsRender)
		{
			return;
		}
		RefreshForegroundSprite();
		if (currentItem == null)
		{
			ClearDisplayedItem();
			ChangeTipPrompt(CurrentPrompt);
			return;
		}
		SingleSpriteRender entity = itemSlot.Entity;
		if (entity == null)
		{
			return;
		}
		Item item = currentItem;
		Sprite sprite = ((item is ItemHat itemHat) ? itemHat.function.HatId_Ref.Preview.Asset : ((item is ItemDroneStructure itemDroneStructure) ? itemDroneStructure.ComposedDroneSprite : ((item is IDroneComponentItem droneComponentItem) ? droneComponentItem.PaddingSprite : ((!(item is ItemAutomateBot itemAutomateBot)) ? currentItem.uiSprite : itemAutomateBot.AutomateBotSprite))));
		Sprite sprite2 = sprite;
		if (sprite2 != null)
		{
			Vector2 vector = new Vector2(28f, 28f) * 0.125f;
			Vector3 positionBottom = base.PositionBottom;
			positionBottom.x += func.IconOffset.x * 0.125f;
			float num = 0.125f + vector.y * 0.6f + base.Height + func.IconOffset.y * 0.125f;
			positionBottom.y += num;
			positionBottom.z = base.Position.y - positionBottom.y - 0.0001f;
			entity.SetVisible(value: true);
			entity.position = positionBottom;
			entity.sprite = sprite2;
			if (func.UseEffects)
			{
				particleEffects?.Recycle();
				particleEffects = DolocAPI.effectProvider.RaiseContinuesPS(base.PositionTop, ContinuesParticleEffectsType.STARS);
				if (particleEffects != null)
				{
					particleEffects.position = positionBottom;
				}
				entity.SpriteRenderer.ToggleItemShine(LocMaterials.GAME_MAT_FLOAT);
				entity.Color = new Color(1f, 0.6451414f, 0f, Random.value);
			}
		}
		ChangeTipPrompt(CurrentPrompt);
	}

	private void RefreshForegroundSprite()
	{
		SingleSpriteRender entity = foregroundSlot.Entity;
		if (func.ForegroundSprite.Asset != null)
		{
			Vector3 vector = position;
			vector.z -= 0.001f;
			entity.SetVisible(value: true);
			entity.position = vector;
			entity.sprite = func.ForegroundSprite.Asset;
		}
		else
		{
			entity.SetVisible(value: false);
		}
	}

	private void ClearDisplayedItem()
	{
		particleEffects?.Recycle();
		particleEffects = null;
		itemSlot.Release();
		foregroundSlot.Release();
	}

	protected override void Update()
	{
		if (func.UseEffects && currentItem != null && particleCounter.Tick())
		{
			DolocAPI.RaiseInstantPSEffects(itemSlot.Entity.position + Vector3.forward, InstantParticleEffectsType.SLOW_BLUE_PARTICLES, "Default", 0);
		}
	}

	protected override void OnRender()
	{
		RefreshDisplayedItem();
	}

	protected override void OnUnRender()
	{
		ClearDisplayedItem();
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		this.PlaceItemInBagOrCreateDropItem(currentItem, putInBackpack, sendMessage: false);
		currentItem = null;
	}

	protected override void OnTouch()
	{
		this.ShowSceneOperationTip(PositionTip, CurrentPrompt, DolocAPI.UserInput.GlobalInteractActionName);
	}

	protected override void OnInteract()
	{
		if (currentItem != null)
		{
			if (TryTakeOffItem())
			{
				RefreshDisplayedItem();
			}
		}
		else
		{
			currentItem = DolocAPI.SelectedItem?.CostSelf();
			RefreshDisplayedItem();
		}
	}

	private bool TryTakeOffItem()
	{
		if (currentItem == null)
		{
			return true;
		}
		if (!currentItem.disposable)
		{
			if (DolocAPI.TryPlaceInBackpack(currentItem))
			{
				currentItem = null;
				return true;
			}
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
			return false;
		}
		this.CreateDropItem(currentItem, base.IsRender, sendMessage: false);
		currentItem = null;
		return true;
	}

	protected override void OnDisTouch()
	{
		this.HideSceneOperationTip();
	}

	public override string GetOccupyInfo()
	{
		return DolocConfig.StaticTexts.FarmbuilderErrShowCaseOccupied;
	}
}
