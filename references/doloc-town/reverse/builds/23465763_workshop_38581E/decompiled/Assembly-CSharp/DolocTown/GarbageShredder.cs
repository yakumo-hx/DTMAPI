using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.Recipe;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class GarbageShredder : EquipmentWorker, IContainer
{
	private string latestItemName;

	public string title => proto.Title;

	public override bool IsValid
	{
		get
		{
			if (base.IsValid)
			{
				return recipeGroupProto != null;
			}
			return false;
		}
	}

	[JsonProperty]
	public LinearInventory inventory { get; protected set; }

	protected EquipmentFuncGarbageShredder func => proto.Function as EquipmentFuncGarbageShredder;

	public int totalCapacity => 1;

	public int lineCapacity => 1;

	public override bool IsDirty => inventory.filledCount > 0;

	private DismantleRecipeGroupInfo recipeGroupProto => func.RecipeGroupName_Ref;

	public GarbageShredder(IEquipmentHost room, int instanceId, EquipmentInfo proto, Vector3 worldPos, Vector2Int anchor, bool turn)
		: base(room, instanceId, proto, worldPos, anchor, turn)
	{
		inventory = new LinearInventory(totalCapacity);
	}

	[JsonConstructor]
	public GarbageShredder(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, LinearInventory inventory)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		this.inventory = inventory ?? new LinearInventory(totalCapacity);
	}

	protected override void OnWorkDoneNoRender()
	{
		base.OnWorkDoneNoRender();
		OnWorkDoneInternal(isRender: false);
	}

	protected override void OnWorkDone()
	{
		base.OnWorkDone();
		PlaySound(value: false);
		OnWorkDoneInternal(isRender: true);
	}

	private bool TryGetRecipe(string itemName, out DismantleRecipeInfo recipeProto)
	{
		recipeProto = null;
		if (itemName.IsNullOrEmpty())
		{
			return false;
		}
		foreach (DismantleRecipeInfo item in recipeGroupProto.RecipeIds_Ref)
		{
			if (item.InputItems.Any((CountItem x) => x.itemName == itemName))
			{
				recipeProto = item;
				return true;
			}
		}
		return false;
	}

	public bool ContentFilter(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (item is ItemEquipment item2)
		{
			return DolocAPI.IsItemSalable(item2);
		}
		DismantleRecipeInfo recipeProto;
		return TryGetRecipe(item.name, out recipeProto);
	}

	protected override void OnRender()
	{
		base.OnRender();
		if (base.IsWorking)
		{
			PlaySound(value: true);
		}
	}

	protected override void OnUnRender()
	{
		base.OnUnRender();
		PlaySound(value: false);
	}

	private void PlaySound(bool value)
	{
		if (!func.InFarm && base.Renderer != null)
		{
			DolocAPI.Sound.PostSoundEvent(value ? SoundEvents.PLAY_GARBAGE_RECYCLING : SoundEvents.STOP_GARBAGE_RECYCLING);
		}
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		ShowTip(DolocConfig.StaticTexts.UiOperationUse);
	}

	protected override void OnDisTouch()
	{
		base.OnDisTouch();
		HideTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		PushTip();
		Item selectedItem = DolocAPI.SelectedItem;
		if (func.InFarm)
		{
			if (DolocAPI.userSettings.useQuickCraft && selectedItem != null && !base.IsWorking && ContentFilter(selectedItem))
			{
				int count2 = Mathf.Min(selectedItem.count, func.RecipeGroupName_Ref.MaxCraftCount);
				DolocAPI.CostSelectedItem(count2);
				inventory.PlaceItem(selectedItem.Clone(count2));
				DolocAPI.agent._Interact();
				RestartWork(isRender: true);
				DolocAPI.DelayFrame(DolocAPI.ReQuickSelectCurrentItem);
			}
			else
			{
				latestItemName = inventory.FirstItemName;
				DolocAPI.EnterUI((GarbageShredderUiState state) => state.HandleStartUpArgs(this, func.RecipeGroupName_Ref.MaxCraftCount, GetTimeInfo, OnContainerUiExit));
			}
			return;
		}
		if (base.IsWorking && !inventory.isEmpty)
		{
			DolocAPI.ShowMessageBoxSmall(DolocUtils.Format(DolocConfig.StaticTexts.UiTipResolving, DolocAPI.GetItemTitle(inventory.FirstItemName)));
			return;
		}
		if (!DolocAPI.CanAffordMoney(func.GoldCost))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipShredderMoney.Format(func.GoldCost));
			return;
		}
		if (ContentFilter(selectedItem))
		{
			PushTipToHide();
			DolocAPI.ShowQuestionBox((func.GoldCost > 0) ? DolocConfig.StaticTexts.UiShredderConfirm.Format(1, selectedItem.title, func.GoldCost) : DolocConfig.StaticTexts.UiShredderConfirmUpgrade.Format(1, selectedItem.title), delegate
			{
				if (MoneyCheck())
				{
					DolocAPI.CostSelectedItem();
					inventory.PlaceItem(selectedItem.Clone(1));
					RestartWork(isRender: true);
				}
			});
			return;
		}
		bool exit = true;
		DolocAPI.archiveHandle.InventorySystem.ForEach(delegate(Item x)
		{
			if (ContentFilter(x))
			{
				exit = false;
			}
		});
		if (!exit)
		{
			DolocAPI.OpenSubmitSingleItemPanel(ContentFilter, MoneyCheck, (Item _) => 1, (Item item, int count) => (func.GoldCost <= 0) ? DolocConfig.StaticTexts.UiShredderConfirmUpgrade.Format(count, item?.title ?? "") : DolocConfig.StaticTexts.UiShredderConfirm.Format(count, item?.title ?? "", func.GoldCost), delegate
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.GarbageSubmitErrItem);
			}, ForceShredItem);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiTipErrShredderEmpty);
		}
	}

	private int GetCurrentInterval()
	{
		if (!ContentFilter(inventory.FirstItem))
		{
			return 0;
		}
		if (!TryGetRecipe(inventory.FirstItem?.name, out var recipeProto))
		{
			return func.Interval;
		}
		return Mathf.RoundToInt(recipeGroupProto.TimeRatio * (float)recipeProto.CostTime);
	}

	protected string GetTimeInfo()
	{
		int currentInterval = GetCurrentInterval();
		if (currentInterval == 0 || inventory.isEmpty)
		{
			return "";
		}
		int num = currentInterval * inventory.FirstItem.count;
		if (latestItemName == inventory.FirstItemName)
		{
			num -= base.WorkCounter.Value / DolocAPI.GlobalParameter.TULength;
		}
		string formatTimeLengthByTU = DolocAPI.GetFormatTimeLengthByTU(num);
		if (formatTimeLengthByTU.IsNullOrEmpty())
		{
			return "";
		}
		return DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelRestTime, formatTimeLengthByTU.Colored(DolocUiColor.TEXTCOLOR_STD) ?? "");
	}

	private bool MoneyCheck()
	{
		if (func.GoldCost <= 0)
		{
			return true;
		}
		if (!DolocAPI.CanAffordMoney(func.GoldCost))
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrMoneyNotEnough);
			return false;
		}
		DolocAPI.archiveHandle.CurrentMoney -= func.GoldCost;
		return true;
	}

	private void ForceShredItem(int index, int count)
	{
		Item item = DolocAPI.archiveHandle.InventorySystem.inventory.Read(index);
		if (item != null)
		{
			inventory.PlaceItem(item.Clone(count));
			DolocAPI.CostItemAt(index, count);
			RestartWork(isRender: true);
		}
	}

	private void OnContainerUiExit()
	{
		if (!(latestItemName == inventory.FirstItemName))
		{
			ForceStopCurrentWork();
			RestartWork(isRender: true);
		}
	}

	private void RestartWork(bool isRender)
	{
		if (inventory == null || inventory.isEmpty)
		{
			latestItemName = null;
			ForceStopCurrentWork();
			return;
		}
		if (isRender)
		{
			DolocAPI.RaiseSpriteFadeUp((Vector2)base.Position + new Vector2(0f, 4.5f), DolocAPI.GetItemSprite(inventory.FirstItemName));
			PlaySound(value: true);
		}
		int currentInterval = GetCurrentInterval();
		if (isRender)
		{
			PlaySound(value: true);
			Work(currentInterval);
		}
		else
		{
			WorkNoRender(currentInterval);
		}
	}

	private void OnWorkDoneInternal(bool isRender)
	{
		if (inventory == null || inventory.isEmpty)
		{
			return;
		}
		Item firstItem = inventory.FirstItem;
		CountItem[] array = null;
		if (TryGetRecipe(inventory.FirstItem?.name, out var recipeProto))
		{
			array = SpawnItems(recipeProto);
		}
		else if (firstItem is ItemEquipment equipment)
		{
			array = SpawnItems(equipment, func.RecycleFactor).ToArray();
		}
		if (array == null)
		{
			this.CreateDropItem(firstItem, isRender, sendMessage: true);
			DolocAPI.outputError("垃圾分解机:无法分解道具\"" + firstItem.name + ">\"");
			return;
		}
		inventory.TryCostAtIndex(0, 1);
		DropAsync(array).Forget();
		if (!inventory.isEmpty)
		{
			RestartWork(isRender);
		}
	}

	private static IEnumerable<CountItem> SpawnItems(ItemEquipment equipment, float recycleFactor)
	{
		int totalCount = 0;
		if (equipment == null)
		{
			yield break;
		}
		CountItem[] costList = equipment.EquipmentProto.CostList;
		CountItem[] array = costList;
		for (int i = 0; i < array.Length; i++)
		{
			CountItem countItem = array[i];
			float num = (float)countItem.itemCount * recycleFactor;
			int num2 = Mathf.FloorToInt(num);
			float num3 = num - (float)num2;
			if (num3 > 0f && Random.value < num3)
			{
				num2++;
			}
			if (num2 > 0)
			{
				string itemName = countItem.itemName;
				totalCount += num2;
				yield return new CountItem(itemName, num2);
			}
		}
		if (totalCount == 0 && costList.Length != 0)
		{
			int num4 = new Vector2Int(0, costList.Length - 1).Random();
			CountItem countItem2 = costList[num4];
			yield return new CountItem(countItem2.itemName, 1);
		}
	}

	private static CountItem[] SpawnItems(DismantleRecipeInfo recipeProto)
	{
		ItemSpawnEntry outputItemSpawnEntry = recipeProto.OutputItemSpawnEntry;
		if (outputItemSpawnEntry.SpawnLut_Ref == null)
		{
			DolocAPI.outputError("垃圾分解机:掉落库\"" + outputItemSpawnEntry.SpawnLut + "\"不存在");
			return null;
		}
		return outputItemSpawnEntry.SpawnLut_Ref.SpawnItems(outputItemSpawnEntry.CountRange.MinCount, outputItemSpawnEntry.CountRange.MaxCount);
	}

	private async UniTaskVoid DropAsync(CountItem[] items)
	{
		for (int j = 0; j < items.Length; j++)
		{
			CountItem item = items[j];
			for (int i = 0; i < item.itemCount; i++)
			{
				this.CreateDropItem(item.itemName, base.IsRender, sendMessage: true);
				await UniTask.Delay(200);
			}
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		if (inventory != null && !inventory.isEmpty)
		{
			Item[] array = inventory.ReadAll();
			foreach (Item item in array)
			{
				this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
			}
			inventory.Clear();
		}
	}
}
