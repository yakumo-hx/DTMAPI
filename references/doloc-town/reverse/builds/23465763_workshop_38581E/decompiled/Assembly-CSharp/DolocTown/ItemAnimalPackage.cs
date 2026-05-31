using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemAnimalPackage : Item
{
	[JsonProperty]
	private Animal animal;

	private readonly ItemFunctionAnimalPackage _func;

	public override string title
	{
		get
		{
			if (_func.PresetAnimal_Ref == null)
			{
				if (!isEmpty)
				{
					return DolocUtils.Format(DolocConfig.StaticTexts.ItemTitleInfoFormat, base.proto.Title, animal.Title);
				}
				return base.proto.Title;
			}
			return base.proto.Title;
		}
	}

	public override string description
	{
		get
		{
			if (!isEmpty)
			{
				return animal.DescriptionInSack;
			}
			return base.description;
		}
	}

	public override Sprite uiSprite => (isEmpty ? base.proto.UiSpriteAsset.Asset : _func.UiSpriteCatch.Asset) ?? base.proto.UiSpriteAsset.Asset;

	public override ItemSubTypeInfo subType
	{
		get
		{
			if (!isEmpty)
			{
				return _func.TypeWhenFull_Ref;
			}
			return base.subType;
		}
	}

	public override bool noOverlay
	{
		get
		{
			if (!base.noOverlay)
			{
				return isFull;
			}
			return true;
		}
	}

	public bool isFull => animal != null;

	public bool isEmpty => animal == null;

	public ItemAnimalPackage(ItemInfo proto, int count)
		: base(proto, count)
	{
		_func = (ItemFunctionAnimalPackage)proto.Function;
		if (_func.PresetAnimal_Ref != null)
		{
			animal = new Animal(_func.PresetAnimal_Ref, DolocAPI.archiveHandle.DateNow);
		}
	}

	[JsonConstructor]
	protected ItemAnimalPackage(string itemName, int itemCount, Animal animal = null)
		: base(itemName, itemCount)
	{
		if (base.proto != null)
		{
			_func = (ItemFunctionAnimalPackage)base.proto.Function;
			this.animal = animal;
		}
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		InitCellTip();
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	private void InitCellTip()
	{
		if (base.isQuickSelected && isEmpty && base.allowAnimalAppear)
		{
			ShowCellTip(Vector2Int.zero, Vector2Int.one, flipWhenFaceLeft: true);
			RefreshCellTip();
		}
		else
		{
			HideCellTip();
		}
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = DolocAPI.CurrentAnimal != null;
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		OnUse();
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		OnUse();
	}

	private void OnUse()
	{
		if (isFull)
		{
			animal.AfterLoadData();
			TryReleaseAnimal();
		}
		else
		{
			TryCatchAnimal();
		}
	}

	private void TryReleaseAnimal()
	{
		if (isEmpty)
		{
			return;
		}
		Room currentHome = DolocAPI.CurrentRoom;
		if (!currentHome.IsInHouse)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.AnimalInvalidRoom);
			return;
		}
		if (!currentHome.IsAnimalBuilding)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.AnimalInvalidBuilding);
			return;
		}
		if (!((IAnimalHost)currentHome).CheckAnimalSpace(animal.proto.Space))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.AnimalFullBuilding);
			return;
		}
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_ANIMAL_SACK_RELEASE);
		DolocAPI.agent._Interact(delegate
		{
			if (animal.homeRoom == null)
			{
				animal?.InvokeChangeNameInputBox(delegate
				{
					string protoName = animal.protoName;
					AddAnimalToCurrentRoom();
					DolocAPI.Broadcast(OperationEventType.USE_ITEM);
					DolocAPI.BroadcastString(GameEventType.USE_ITEM, base.proto.Id);
					DolocAPI.archiveHandle.RecordCollection(CollectionType.Animal, protoName);
				});
			}
			else
			{
				AddAnimalToCurrentRoom();
			}
		});
		void AddAnimalToCurrentRoom()
		{
			CostSelf(out var item);
			((IAnimalHost)currentHome).AddAnimal(animal, DolocAPI.AgentRoomCellPosition + new Vector2Int(DolocAPI.AgentFaceRight ? 1 : (-2), 0));
			Vector3 vector = animal.PositionWSOfCell + new Vector3(0f, 1.5f);
			DolocAPI.RaiseInstantPSEffects(animal.PositionWSOfCell - new Vector3(0f, 1.5f), InstantParticleEffectsType.LIGHT_SPARKS);
			animal = null;
			EmitSelf();
			if (_func.Reusable)
			{
				DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, item, vector, shouldSendMsg: false);
			}
			InitCellTip();
		}
	}

	private void TryCatchAnimal()
	{
		if (isFull)
		{
			return;
		}
		if (DolocAPI.CurrentAnimal == null)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.AnimalNoAnimal);
			return;
		}
		if (count > 1 && DolocAPI.archiveHandle.InventorySystem.inventory.emptyCount == 0)
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
			return;
		}
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_ANIMAL_SACK_CATCH);
		DolocAPI.agent._Interact(delegate
		{
			Animal currentAnimal = DolocAPI.CurrentAnimal;
			Item item;
			if (currentAnimal == null)
			{
				DolocAPI.RaiseEmotionLimited(DolocAPI.AgentTransform, EmotionName.EMBARRASSED);
			}
			else if (count > 1 && DolocAPI.archiveHandle.InventorySystem.inventory.emptyCount == 0)
			{
				DolocAPI.RaiseEmotionLimited(DolocAPI.AgentTransform, EmotionName.EMBARRASSED);
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
			}
			else if (CostSelf(out item) && item is ItemAnimalPackage itemAnimalPackage)
			{
				Vector3 vector = currentAnimal.PositionWSOfCell + new Vector3(0f, 1.5f);
				if (!((IAnimalHost)currentAnimal.homeRoom).RemoveAnimal(currentAnimal))
				{
					Debug.LogError("AnimalRenderer._HandleInteract: 无法删除小动物，捕捉失败");
				}
				else
				{
					itemAnimalPackage.animal = currentAnimal;
					DolocAPI.RaiseSpriteFadeDown(vector, itemAnimalPackage.uiSprite);
					DolocAPI.RaiseInstantPSEffects(vector, InstantParticleEffectsType.BRUST_STARS);
					EmitSelf();
					DolocAPI.TryPlaceInBackpack(itemAnimalPackage);
					InitCellTip();
				}
			}
		});
	}

	public override bool IsSame(Item other)
	{
		if (!base.IsSame(other))
		{
			return false;
		}
		if (!(other is ItemAnimalPackage itemAnimalPackage))
		{
			return false;
		}
		if (isEmpty)
		{
			return itemAnimalPackage.isEmpty;
		}
		return false;
	}

	public override Item Clone(int count)
	{
		return new ItemAnimalPackage(name, count, animal);
	}

	protected override bool CanBuyback()
	{
		return isEmpty;
	}

	protected override bool CanDispose()
	{
		return isEmpty;
	}

	protected override bool CanPutInToContainer()
	{
		return isEmpty;
	}

	protected override int GetSellingPrice()
	{
		if (isEmpty)
		{
			return base.GetSellingPrice();
		}
		if (!animal.IsAdult)
		{
			return animal.proto.ChildPrice;
		}
		return animal.proto.AdultPrice;
	}

	public override string GetExtraInfo1()
	{
		if (animal == null)
		{
			return "";
		}
		string arg = (animal.IsAdult ? DolocConfig.StaticTexts.UiAnimalAdult : DolocConfig.StaticTexts.UiAnimalChild);
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemTitleInfoFormat, animal.proto.Title, arg);
	}

	public override string GetExtraInfo2()
	{
		if (animal == null)
		{
			return "";
		}
		return animal.BirthdayInfo;
	}
}
