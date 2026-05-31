using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.TechTree;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class Synthesizer : EquipmentWorker, IContainer
{
	protected RecipeGroup recipeGroup;

	protected DishGroup dishGroup;

	[JsonProperty]
	protected IRecipe latestRecipe;

	[JsonProperty]
	private IRecipeGroup latestRecipeGroup;

	[JsonProperty]
	protected LinearInventory itemBuffer;

	[JsonProperty]
	protected Counter taskCounter;

	private bool? _useConversionMode;

	protected CountItem latestItem;

	public string title => proto.Title;

	protected EquipmentFuncSynthesizerBase func => proto.Function as EquipmentFuncSynthesizerBase;

	public override bool IsValid
	{
		get
		{
			if (base.IsValid)
			{
				if (recipeGroup == null)
				{
					return dishGroup != null;
				}
				return true;
			}
			return false;
		}
	}

	protected bool firstFocusOnRecipe => latestRecipeGroup is RecipeGroup;

	private bool inFixedTask
	{
		get
		{
			if (base.IsWorking)
			{
				return latestRecipeGroup is RecipeGroup;
			}
			return false;
		}
	}

	private bool inDynamicTask
	{
		get
		{
			if (base.IsWorking)
			{
				return latestRecipeGroup is DishGroup;
			}
			return false;
		}
	}

	protected LinearInventory backpack => DolocAPI.archiveHandle.InventorySystem.inventory;

	public string RecipeGroupName => recipeGroup.GroupId;

	public bool IsProcessing => base.IsWorking;

	public override int WorkScale => taskCounter?.Interval ?? 0;

	public bool CanEditBuffer
	{
		get
		{
			if (base.IsWorking)
			{
				return !(latestRecipeGroup?.IsFixedDuration ?? false);
			}
			return true;
		}
	}

	private int unfinishedTaskCount
	{
		get
		{
			if (taskCounter != null)
			{
				return taskCounter.Interval - taskCounter.Value;
			}
			return 0;
		}
	}

	protected virtual bool useConversionMode
	{
		get
		{
			bool valueOrDefault = _useConversionMode.GetValueOrDefault();
			if (!_useConversionMode.HasValue)
			{
				valueOrDefault = func.UseConversionMode && dishGroup == null && ((IRecipeGroup)recipeGroup).GetAllRecipes(includeLocked: true).All((IRecipe x) => x.InputItems.Length == 1);
				_useConversionMode = valueOrDefault;
			}
			return _useConversionMode.Value;
		}
	}

	public LinearInventory inventory => itemBuffer;

	public int totalCapacity => 1;

	public int lineCapacity => 1;

	public void AutomateCraft(IRecipe recipe, int maxScale)
	{
		OnCraft(recipe, recipeGroup, maxScale);
	}

	public Synthesizer(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		InitRecipeGroup();
		ValidateTimeAddition();
		if (dishGroup != null)
		{
			itemBuffer = new LinearInventory(dishGroup.proto.SlotCount);
		}
		else if (useConversionMode)
		{
			itemBuffer = new LinearInventory(1);
		}
	}

	[JsonConstructor]
	protected Synthesizer(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, bool isIdle, bool isWorking, Counter counter, IRecipe latestRecipe, IRecipeGroup latestRecipeGroup, LinearInventory itemBuffer, Counter taskCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, isIdle, isWorking, counter)
	{
		if (func == null)
		{
			return;
		}
		this.taskCounter = taskCounter;
		this.latestRecipe = ((latestRecipe != null && latestRecipe.isValid) ? latestRecipe : null);
		this.latestRecipeGroup = ((latestRecipeGroup != null && latestRecipeGroup.isValid) ? latestRecipeGroup : null);
		this.itemBuffer = itemBuffer;
		InitRecipeGroup();
		if (dishGroup != null)
		{
			int slotCount = dishGroup.proto.SlotCount;
			if (this.itemBuffer == null)
			{
				this.itemBuffer = new LinearInventory(slotCount);
			}
			this.itemBuffer.ValidateCapacity(slotCount);
			if (this.itemBuffer.capacity != slotCount)
			{
				LinearInventory linearInventory = new LinearInventory(slotCount);
				for (int i = 0; i < Mathf.Min(slotCount, this.itemBuffer.capacity); i++)
				{
					linearInventory.PlaceItemAt(i, this.itemBuffer.Read(i)?.Clone());
				}
				this.itemBuffer = linearInventory;
			}
		}
		else if (useConversionMode)
		{
			if (this.itemBuffer == null)
			{
				this.itemBuffer = new LinearInventory(1);
				ValidateSpecialRecipe(latestRecipe?.RecipeId);
			}
			this.itemBuffer.ValidateCapacity(1);
		}
	}

	private void ValidateSpecialRecipe(string recipeId)
	{
		if (recipeId.IsNullOrEmpty() || unfinishedTaskCount <= 0)
		{
			return;
		}
		foreach (var (text, name, name2) in new List<(string, string, string)>
		{
			("big_rice_wine", "rice_wine", "rice"),
			("big_beer", "beer", "beer_flower"),
			("big_wine", "wine", "grape"),
			("big_wine_agave", "wine_agave", "agave")
		})
		{
			if (recipeId == text)
			{
				DolocAPI.QueryRecipe(name, out var recipe);
				latestRecipe = recipe;
				itemBuffer.PlaceItem(DolocAPI.GenerateItem(name2, 10 * unfinishedTaskCount));
				taskCounter = new Counter(inventory.FirstItem.count);
				return;
			}
		}
		foreach (var (text2, name3) in new List<(string, string)>
		{
			("aged_rice_wine", "rice_wine"),
			("aged_beer", "beer"),
			("aged_wine", "wine"),
			("aged_wine_agave", "wine_agave")
		})
		{
			if (recipeId == text2)
			{
				itemBuffer.PlaceItem(DolocAPI.GenerateItem(name3, 6 * unfinishedTaskCount));
				taskCounter = new Counter(inventory.FirstItem.count);
				break;
			}
		}
	}

	protected override void AfterSetHost()
	{
		base.AfterSetHost();
		ValidateTimeAddition();
	}

	private void ValidateTimeAddition()
	{
		float value = 0f;
		base.CurrentRoom?.RoomEffectInfo.SynthesizerTimeAdditions.TryGetValue(base.Name ?? "", out value);
		if (recipeGroup != null)
		{
			recipeGroup.TimeAddition = value;
		}
		if (latestRecipeGroup != null)
		{
			latestRecipeGroup.TimeAddition = value;
		}
		if (dishGroup != null)
		{
			dishGroup.TimeAddition = value;
		}
	}

	private void InitRecipeGroup()
	{
		recipeGroup = new RecipeGroup(func.RecipeGroupName_Ref);
		dishGroup = new DishGroup(func.DishGroupName_Ref);
		if (!recipeGroup.isValid)
		{
			recipeGroup = null;
		}
		if (!dishGroup.isValid)
		{
			dishGroup = null;
		}
	}

	protected override void OnTouch()
	{
		if (!base.IsWorking)
		{
			ShowTip(DolocConfig.StaticTexts.UiOperationInteract);
		}
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}

	protected override void OnInteract()
	{
		PushTipToHide();
		if (TryQuickStart())
		{
			return;
		}
		GetTaskInfo(out var taskInfo, out var taskTitle);
		if (useConversionMode)
		{
			latestItem = new CountItem(inventory.FirstItem);
			DolocAPI.EnterUI((ConversionRecipeUiState state) => state.HandleStartUpArgs(recipeGroup, this, CanEditBuffer, taskTitle, GetTimeInfo, OnCraft));
		}
		else
		{
			DolocAPI.EnterUI((RecipePanelUiState state) => state.HandleStartUpArgs(recipeGroup, dishGroup, firstFocusOnRecipe, itemBuffer, DolocAPI.GetInventoriesAroundEquipment(this), latestRecipe?.RecipeId, closeAfterCraft: true, GetMaxCraftCount, OnCraft, OnExitUI, taskInfo, taskTitle, inFixedTask, inDynamicTask));
		}
	}

	protected bool TryQuickStart()
	{
		if (!DolocAPI.userSettings.useQuickCraft || base.IsWorking)
		{
			return false;
		}
		Item selectedItem = DolocAPI.SelectedItem;
		if (selectedItem != null && ContentFilter(selectedItem))
		{
			LinearInventory[] inventoriesAroundEquipment = DolocAPI.GetInventoriesAroundEquipment(this);
			List<Recipe> allRecipes = recipeGroup.GetAllRecipes(includeLocked: false);
			List<Recipe> list = new List<Recipe>();
			if (allRecipes.Count > 0)
			{
				foreach (Recipe item in allRecipes)
				{
					CountItem[] inputItems = item.InputItems;
					for (int i = 0; i < inputItems.Length; i++)
					{
						CountItem countItem = inputItems[i];
						if (selectedItem.name == countItem.itemName && ((IRecipe)item).AffordCostInputItemsInInventory(inventoriesAroundEquipment, 1))
						{
							list.Add(item);
							break;
						}
					}
				}
			}
			if (list.Count > 0)
			{
				if (list.Count > 1 && DolocAPI.userSettings.openMenuWhenRecipeConflict)
				{
					latestRecipe = list.First();
					latestRecipeGroup = recipeGroup;
				}
				else
				{
					Recipe recipe = (DolocAPI.userSettings.selectFirstRecipe ? list.First() : list.Last());
					if (QuickStartInternal(recipe, selectedItem, inventoriesAroundEquipment))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	protected virtual bool QuickStartInternal(IRecipe recipe, Item selectedItem, LinearInventory[] inventories)
	{
		if (base.IsWorking || selectedItem == null)
		{
			return false;
		}
		int maxCraftCount = GetMaxCraftCount(recipe, selectedItem, inventories);
		Item item = selectedItem.Clone(maxCraftCount);
		if (useConversionMode)
		{
			if (!inventory.isEmpty)
			{
				return false;
			}
			latestItem = new CountItem(inventory.FirstItem);
			inventory.PlaceItemAt(0, item);
		}
		CountItem[] inputItems = recipe.InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			Item firstItemWithName = GetFirstItemWithName(countItem.itemName, selectedItem, inventories);
			inventories.MaxCostItem(firstItemWithName, countItem.itemCount * maxCraftCount, useConversionMode, DolocAPI.SelectedItemIndex);
		}
		DolocAPI.agent._Interact();
		DolocAPI.DelayFrame(DolocAPI.ReQuickSelectCurrentItem);
		OnCraft(recipe, recipeGroup, maxCraftCount);
		OnExitUI(recipe, recipeGroup);
		return true;
	}

	protected int GetMaxCraftCount(IRecipe recipe, Item selectedItem, LinearInventory[] inventories)
	{
		int num = int.MaxValue;
		CountItem[] inputItems = recipe.InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			Item firstItemWithName = GetFirstItemWithName(countItem.itemName, selectedItem, inventories);
			num = Mathf.Min(inventories.CountItem(firstItemWithName, useConversionMode) / Mathf.Max(1, countItem.itemCount), num);
		}
		if (recipeGroup.MaxCraftCount > 0)
		{
			return Mathf.Min(recipeGroup.MaxCraftCount, num);
		}
		return num;
	}

	protected Item GetFirstItemWithName(string itemName, Item selectedItem, LinearInventory[] inventories)
	{
		if (!(selectedItem?.name == itemName))
		{
			return inventories.GetFirstItemWithName(itemName);
		}
		return selectedItem;
	}

	protected string GetTimeInfo(IRecipeGroup recipeGroup, IRecipe recipe, int scale)
	{
		if (recipeGroup == null || recipe == null)
		{
			return "";
		}
		float num = recipeGroup.GetRecipeTime(recipe, scale);
		if (latestRecipe?.RecipeId == recipe.RecipeId)
		{
			num -= (float)base.WorkCounter.Value / (float)DolocAPI.GlobalParameter.TULength;
		}
		string str = (DolocAPI.archiveHandle.farmData.recipeManager.CheckRecipeUnlocked(recipe.RecipeId, includeDish: true) ? DolocAPI.GetFormatTimeLengthByTU(Mathf.CeilToInt(num)) : "???");
		return DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelRestTime, str.Colored(DolocUiColor.TEXTCOLOR_STD) ?? "");
	}

	protected void GetTaskInfo(out string taskInfo, out string taskTitle)
	{
		taskInfo = "";
		taskTitle = "";
		if (base.IsWorking)
		{
			float f = (float)(latestRecipeGroup.GetRecipeTime(latestRecipe) * (taskCounter.Interval - taskCounter.Value)) - (float)((IEquipmentWorker)this).WorkCounter.Value / (float)DolocAPI.GlobalParameter.TULength;
			bool flag = DolocAPI.archiveHandle.farmData.recipeManager.CheckRecipeUnlocked(latestRecipe.RecipeId, includeDish: true);
			taskTitle = (flag ? latestRecipe.RecipeTitle : "???");
			string str = (flag ? DolocAPI.GetFormatTimeLengthByTU(Mathf.CeilToInt(f)) : "???");
			taskInfo = DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelTask, $"{taskTitle.Colored(DolocUiColor.EYECATCHCOLOR_CYAN)} ({taskCounter})".Colored(DolocUiColor.TEXTCOLOR_STD));
			string text = DolocUtils.Format(DolocConfig.StaticTexts.RecipePanelRestTime, str.Colored(DolocUiColor.TEXTCOLOR_STD) ?? "");
			taskInfo = taskInfo + "\n" + text;
		}
	}

	protected int GetRecipeTime(IRecipe recipe, int scale = 1)
	{
		return latestRecipeGroup.GetRecipeTime(recipe, scale);
	}

	protected void OnCraft(IRecipe recipe, IRecipeGroup group, int scale)
	{
		if (recipe == null)
		{
			taskCounter = null;
			ForceStopCurrentWork();
			return;
		}
		if (useConversionMode && !inventory.isEmpty && latestItem.itemName == inventory.FirstItem.name)
		{
			taskCounter = new Counter(inventory.FirstItem.count);
			return;
		}
		if (useConversionMode)
		{
			ForceStopCurrentWork();
		}
		latestRecipe = recipe;
		latestRecipeGroup = group;
		StartWork(recipe, scale, isRender: true, 0.5f);
	}

	protected void OnStartWorkInContainerMode(IRecipe recipe, bool isRenderer)
	{
		if (latestRecipe != recipe)
		{
			StartWorkInternal(recipe, isRenderer, 0.5f);
		}
	}

	protected void OnExitUI(IRecipe recipe, IRecipeGroup group)
	{
		if (!base.IsWorking)
		{
			latestRecipe = recipe;
			latestRecipeGroup = group;
		}
	}

	public int GetMaxCraftCountOfInventories(IRecipe recipe, LinearInventory[] inventories)
	{
		if (recipeGroup.MaxCraftCount <= 0)
		{
			return recipe.MaxAffordScale(inventories);
		}
		return Mathf.Min(recipeGroup.MaxCraftCount, recipe.MaxAffordScale(inventories));
	}

	private int GetMaxCraftCount(IRecipe recipe)
	{
		LinearInventory[] inventoriesAroundEquipment = DolocAPI.GetInventoriesAroundEquipment(this);
		return GetMaxCraftCountOfInventories(recipe, inventoriesAroundEquipment);
	}

	protected void StartWork(IRecipe recipe, int scale, bool isRender, float fadeDelay = 0f)
	{
		if (scale < 0)
		{
			taskCounter = null;
			return;
		}
		taskCounter = new Counter(scale);
		StartWorkInternal(recipe, isRender, fadeDelay);
	}

	protected void StartWorkInternal(IRecipe recipe, bool isRender, float fadeDelay = 0f)
	{
		latestRecipe = recipe;
		if (isRender)
		{
			Work(GetRecipeTime(recipe));
			Sprite[] icons = recipe.InputItems.Select((CountItem x) => DolocAPI.GetItemSprite(x.itemName)).ToArray();
			DolocAPI.Delay(fadeDelay, delegate
			{
				DolocAPI.RaiseSpriteArrayFadeUp(base.PositionCenter, icons);
			});
		}
		else
		{
			WorkNoRender(GetRecipeTime(recipe));
		}
	}

	public void Raise(string recipeId, int scale, bool isRender)
	{
		DolocAPI.QueryRecipe(recipeId, out var recipe);
		StartWork(recipe, scale, isRender);
		if (isRender)
		{
			Sprite[] icons = recipe.InputItems.Select((CountItem x) => DolocAPI.GetItemSprite(x.itemName)).ToArray();
			DolocAPI.RaiseSpriteArrayFadeUp(base.PositionCenter, icons);
		}
	}

	protected override void OnWorkDone()
	{
		OnWorkDoneInternal(isRender: true);
	}

	protected override void OnWorkDoneNoRender()
	{
		OnWorkDoneInternal(isRender: false);
	}

	protected virtual void OnWorkDoneInternal(bool isRender)
	{
		if (taskCounter == null)
		{
			return;
		}
		if (latestRecipeGroup is DishGroup)
		{
			itemBuffer.Clear();
			DolocAPI.archiveHandle.UnlockRecipe(latestRecipe?.RecipeId, includeDish: true);
			CreateNextOutputItem(isRender, 1);
		}
		else if (useConversionMode)
		{
			if (latestRecipeGroup.IsFixedDuration && itemBuffer.FirstItem != null)
			{
				CreateNextOutputItem(isRender, itemBuffer.FirstItem.count);
				itemBuffer.Clear();
				taskCounter = null;
				return;
			}
			CreateNextOutputItem(isRender, 1);
			itemBuffer.MaxCost(latestRecipe.InputItems);
		}
		else
		{
			CreateNextOutputItem(isRender, 1);
		}
		SendUseEquipmentMessage();
		if (taskCounter.Tick())
		{
			taskCounter = null;
		}
		else
		{
			StartWorkInternal(latestRecipe, isRender, 0.5f);
		}
	}

	protected void CreateNextOutputItem(bool isRender, int count)
	{
		CountItem countItem = latestRecipe?.GenerateOutputItem() ?? default(CountItem);
		if (latestRecipe is Dish dish && DolocAPI.AgentEquipmentManager.TryGetAgentEquipmentFunction<AgentEquipmentFunctionCook>(out var function) && RandomUtils.Dice(function.func.Probability))
		{
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				num += dish.MaxQualityItem.randomCount;
			}
			countItem = new CountItem(dish.MaxQualityItem.itemName, num);
		}
		if (countItem.itemName.IsNullOrEmpty())
		{
			return;
		}
		DolocAPI.archiveHandle.UnlockRecipe(latestRecipe?.RecipeId);
		for (int j = 0; j < count; j++)
		{
			CreateDropItem(isRender, countItem);
			DolocAPI.AddTechExp(TechPointType.SCIENCE, latestRecipe?.TechPoint ?? 1);
			HashSet<string> hashSet = new HashSet<string>();
			for (int k = 0; k < countItem.itemCount; k++)
			{
				if (hashSet.Add(countItem.itemName))
				{
					TrySendNewFoodMessage(countItem.itemName);
				}
				DolocAPI.BroadcastString(GameEventType.MAKE_ITEM, countItem.itemName);
			}
		}
	}

	protected virtual void CreateDropItem(bool isRender, CountItem countItem)
	{
		this.CreateDropItem(countItem, isRender, sendMessage: true);
	}

	private void TrySendNewFoodMessage(string itemName)
	{
		if (DolocAPI.QueryItemProto(itemName, out var itemInfo) && DolocAPI.GlobalParameter.FoodItemSubTypes.Contains(itemInfo.SubType) && DolocAPI.GetEventTriggerCount(GameEventType.MAKE_ITEM, itemName) <= 0)
		{
			DolocAPI.BroadcastString(GameEventType.MAKE_NEW_FOOD, itemName);
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		if (inFixedTask && !useConversionMode)
		{
			CountItem[] inputItems = latestRecipe.InputItems;
			for (int i = 0; i < inputItems.Length; i++)
			{
				CountItem countItem = inputItems[i];
				for (int j = 0; j < countItem.itemCount * unfinishedTaskCount; j++)
				{
					this.PlaceItemInBagOrCreateDropItem(countItem.itemName, putInBackpack, sendMessage: false);
				}
			}
		}
		if (itemBuffer != null)
		{
			Item[] array = itemBuffer.ReadAll();
			foreach (Item item in array)
			{
				this.PlaceItemInBagOrCreateDropItem(item, putInBackpack, sendMessage: false);
			}
			itemBuffer.Clear();
		}
	}

	public override void OnFunctionChange()
	{
		if (!(base.Name == "seed_compacting_machine") || !inFixedTask || latestRecipe == null)
		{
			return;
		}
		CountItem[] inputItems = latestRecipe.InputItems;
		for (int i = 0; i < inputItems.Length; i++)
		{
			CountItem countItem = inputItems[i];
			for (int j = 0; j < countItem.itemCount * unfinishedTaskCount; j++)
			{
				this.PlaceItemInBagOrCreateDropItem(countItem.itemName, putInBackpack: false, sendMessage: false);
			}
		}
	}

	public virtual bool ContentFilter(Item content)
	{
		if (content == null)
		{
			return false;
		}
		if (content.proto.Function is ItemFunctionSeedMaternal itemFunctionSeedMaternal && !DolocAPI.archiveHandle.IsSeedNodeUnlocked(itemFunctionSeedMaternal.SeedItemId))
		{
			return false;
		}
		return ((IRecipeGroup)recipeGroup).GetAllRecipes(includeLocked: true).Any((IRecipe x) => x.InputItems.Any((CountItem y) => y.itemName == content.name));
	}
}
