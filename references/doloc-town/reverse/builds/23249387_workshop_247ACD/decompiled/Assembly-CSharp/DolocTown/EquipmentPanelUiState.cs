using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Recipe;
using DolocTown.Config.TechTree;
using DolocTown.Config.UI;
using DolocTown.GameData;
using DolocTown.TreeGraph;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class EquipmentPanelUiState : PageUiStateBase<EquipmentPanel, EquipmentData>
{
	private bool _hideOnPause = true;

	private LinearInventory[] inventoriesAround;

	private EquipmentMenuSubType[] _subTypes;

	private string _currentEquipmentName;

	private static EquipmentSortMode _equipmentSortMode = EquipmentSortMode.UNLOCK;

	private static int _currentTypeIndex = 0;

	private Dictionary<EquipmentMenuSubType, List<string>> totalDatas = new Dictionary<EquipmentMenuSubType, List<string>>();

	private List<string> currentEquipments = new List<string>();

	private Tween _anim;

	protected override bool hideOnPause => _hideOnPause;

	protected override int totalCapacity => currentEquipments.Count;

	private TbEquipmentMenu MenuConfig => DolocConfig.Tables.TbEquipmentMenu;

	private List<string> CollectEquipments => DolocAPI.archiveHandle.farmData.collectEquipments;

	private EquipmentMenuSubType CurrentEquipmentType => _subTypes[_currentTypeIndex];

	public bool HandleStartUpArgs(RecipeGroupInfo equipmentGroup, LinearInventory[] inventoriesAround)
	{
		GetAllEquipmentsData(equipmentGroup);
		this.inventoriesAround = inventoriesAround ?? Array.Empty<LinearInventory>();
		return true;
	}

	protected override EquipmentData[] DataGetter(int start, int end)
	{
		List<EquipmentData> list = new List<EquipmentData>();
		int count = currentEquipments.Count;
		for (int i = start; i < Mathf.Min(end, count); i++)
		{
			list.Add(new EquipmentData(currentEquipments[i], inventoriesAround));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		_currentEquipmentName = null;
		Dictionary<EquipmentMenuSubType, EquipmentMenuInfo> dictionary = new Dictionary<EquipmentMenuSubType, EquipmentMenuInfo>(MenuConfig.DataMap);
		if (!DolocAPI.archiveHandle.CheckTechTreeUnlocked(DolocConfig.Tables.TbTechTree.GetByTechPoint(TechPointType.ANIMAL).Id))
		{
			dictionary.Remove(EquipmentMenuSubType.ANIMAL);
		}
		_subTypes = dictionary.Values.Select((EquipmentMenuInfo x) => x.Id).ToArray();
		base.panel.menuUI.Render(dictionary.Values.Select((EquipmentMenuInfo x) => x.IconAsset.Asset).ToArray());
	}

	protected override void Register()
	{
		base.Register();
		base.panel.menuUI.SetClickCallbacks(OnMenuIconClick);
		base.panel.menuUI.leftBtn.onClick.AddListener(OnLeftButtonClick);
		base.panel.menuUI.rightBtn.onClick.AddListener(OnRightButtonClick);
		base.panel.onDataSelect.AddListener(OnDataSelect);
		base.panel.BtnCraft.SetClickCallbacks(OnStartButtonClickLeft, OnStartButtonClickRight, null, null, OnStartButtonLongClickLeft, null, null, OnStartButtonContinuesClickRight);
		base.panel.BtnCollect.onClick.AddListener(OnCollectBtnClick);
		EquipmentPanel equipmentPanel = base.panel;
		equipmentPanel.OnCollectBtnClick = (Action<int>)Delegate.Combine(equipmentPanel.OnCollectBtnClick, new Action<int>(OnCollectBtnClickByIndex));
	}

	protected override void Unregister()
	{
		base.panel.menuUI.RemoveCallbacks();
		base.panel.menuUI.leftBtn.onClick.RemoveListener(OnLeftButtonClick);
		base.panel.menuUI.rightBtn.onClick.RemoveListener(OnRightButtonClick);
		_currentEquipmentName = null;
		base.panel.onDataSelect.RemoveListener(OnDataSelect);
		base.panel.BtnCraft.ClearAllClickCallbacks();
		base.panel.BtnCollect.onClick.RemoveListener(OnCollectBtnClick);
		EquipmentPanel equipmentPanel = base.panel;
		equipmentPanel.OnCollectBtnClick = (Action<int>)Delegate.Remove(equipmentPanel.OnCollectBtnClick, new Action<int>(OnCollectBtnClickByIndex));
		base.Unregister();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (!ContinuouslyPressLast(deltaTime, base.panel.PrevPage) && !ContinuouslyPressNext(deltaTime, base.panel.NextPage))
		{
			if (userInput.BaseSortItem)
			{
				Sort();
			}
			else if (userInput.BasePageUpPressed)
			{
				PrevType();
			}
			else if (userInput.BasePageDownPressed)
			{
				NextType();
			}
			else if (userInput.BaseIsSplitPressed)
			{
				UpdateCollectState(_currentEquipmentName);
			}
			else if (userInput.BaseIsConfirmPressed)
			{
				base.panel.BtnCraft.FireClick();
			}
			else if (userInput.BaseIsCancelPressed)
			{
				gameController.PopState();
			}
		}
	}

	private void PrevType()
	{
		int index = (_currentTypeIndex + _subTypes.Length - 1) % _subTypes.Length;
		base.panel.menuUI.FireClick(index);
	}

	private void NextType()
	{
		int index = (_currentTypeIndex + 1) % _subTypes.Length;
		base.panel.menuUI.FireClick(index);
	}

	private void OnLeftButtonClick(int _)
	{
		PrevType();
	}

	private void OnRightButtonClick(int _)
	{
		NextType();
	}

	private void OnMenuIconClick(int index)
	{
		_currentTypeIndex = index;
		RefreshOnEquipmentTypeChange();
		DolocAPI.UIRaisePage();
	}

	private void RefreshOnEquipmentTypeChange()
	{
		RefreshCurrentDataList();
		Unregister();
		Register();
		base.panel.Select(0);
		UpdateTitle();
		base.panel.RefreshView();
		base.panel.SetEmptyInfo(GetEmptyInfo());
	}

	private void OnDataSelect(int index)
	{
		if (index >= 0 && index < currentEquipments.Count)
		{
			_currentEquipmentName = currentEquipments[index];
			DolocAPI.UIRaiseRoll();
		}
		DolocAPI.HideHoverBox();
	}

	private void OnStartButtonClickLeft(int _)
	{
		StartCraft(single: false, useMaxCount: false);
	}

	private void OnStartButtonLongClickLeft(int _)
	{
		StartCraft(single: false, useMaxCount: true);
	}

	private bool OnStartButtonContinuesClickRight(int _)
	{
		return StartCraft(single: true, useMaxCount: false);
	}

	private void OnStartButtonClickRight(int _)
	{
		StartCraft(single: true, useMaxCount: false);
	}

	private bool StartCraft(bool single, bool useMaxCount)
	{
		base.panel.GetFocus();
		if (_currentEquipmentName.IsNullOrEmpty())
		{
			return false;
		}
		DolocAPI.QueryEquipment(_currentEquipmentName, out var proto);
		if (!proto.BaseEquipment.IsNullOrEmpty() && !DolocAPI.archiveHandle.IsEquipmentUnlocked(proto.BaseEquipment))
		{
			if (GetJumpNodeInfo(proto.BaseEquipment, out var techId, out var nodeName))
			{
				DolocAPI.JumpTechTreeNode(techId, nodeName);
			}
			return false;
		}
		if (!DolocAPI.archiveHandle.IsEquipmentUnlocked(_currentEquipmentName))
		{
			if (GetJumpNodeInfo(_currentEquipmentName, out var techId2, out var nodeName2))
			{
				DolocAPI.JumpTechTreeNode(techId2, nodeName2);
			}
			return false;
		}
		if (base.panel.BtnCraft.grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrMaterialNotEnough);
			if (DolocAPI.gameManager.shouldBuilderCostAssets)
			{
				return false;
			}
		}
		IRecipe recipe = new Recipe(_currentEquipmentName);
		if (!recipe.isValid)
		{
			Debug.LogError("配方<" + _currentEquipmentName + ">不存在");
			return false;
		}
		if (single)
		{
			MakeItem(recipe, 1);
		}
		else
		{
			int maxCount = recipe.MaxAffordScale(inventoriesAround);
			if (maxCount == 0)
			{
				return false;
			}
			_hideOnPause = false;
			DolocAPI.EnterUI((CraftQuantitySubmitUiState state) => state.HandleStartUpArgs(new CraftQuantityData(maxCount, recipe, inventoriesAround.ToArray(), null, (!useMaxCount) ? 1 : maxCount), delegate(int count)
			{
				MakeItem(recipe, count);
			}));
		}
		return true;
	}

	private void OnCollectBtnClick(int _)
	{
		UpdateCollectState(_currentEquipmentName);
		base.panel.GetFocus();
	}

	private void OnCollectBtnClickByIndex(int index)
	{
		if (index >= 0 && index < currentEquipments.Count)
		{
			UpdateCollectState(currentEquipments[index]);
			int num = index;
			if (CurrentEquipmentType == EquipmentMenuSubType.COLLECT)
			{
				num = Mathf.Clamp(num - 1, 0, index);
			}
			base.panel.Select(num);
		}
	}

	private void MakeItem(IRecipe recipe, int count)
	{
		recipe.GenerateOutputItemAsDropItem(count);
		if (DolocAPI.gameManager.shouldBuilderCostAssets)
		{
			recipe.TryCostInputItemsInInventory(inventoriesAround, count);
		}
		base.panel.RefreshView();
		base.panel.RaiseCostItemsFadeUp();
		string arg = DolocUtils.Format(base.staticTexts.UiItemTip, DolocAPI.GetEquipmentTitle(_currentEquipmentName), count);
		DolocAPI.ShowMessageBoxSmall(DolocUtils.Format(base.staticTexts.EquipmentPanelMakeComplete, arg));
		DolocAPI.AddTechExp(TechPointType.SCIENCE, recipe.TechPoint * count);
		for (int i = 0; i < count; i++)
		{
			DolocAPI.BroadcastString(GameEventType.MAKE_ITEM, _currentEquipmentName);
		}
	}

	private void UpdateCollectState(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			if (CollectEquipments.Contains(name))
			{
				CollectEquipments.Remove(name);
			}
			else
			{
				CollectEquipments.Add(name);
			}
			if (CurrentEquipmentType == EquipmentMenuSubType.COLLECT)
			{
				RefreshOnEquipmentTypeChange();
			}
			base.panel.RefreshView();
		}
	}

	private void UpdateTitle()
	{
		string title = MenuConfig.Get(CurrentEquipmentType).Title;
		base.panel.SetTitle(string.Format(base.staticTexts.EquipmentPanelListName, title));
	}

	private string GetEmptyInfo()
	{
		return base.staticTexts.EquipmentPanelEmpty;
	}

	private void ShowSortInfo()
	{
		string msg = "";
		switch (_equipmentSortMode)
		{
		case EquipmentSortMode.DEFAULT:
			msg = base.staticTexts.UiSortDefault;
			break;
		case EquipmentSortMode.UNLOCK:
			msg = base.staticTexts.UiSortUnlock;
			break;
		}
		DolocAPI.ShowMessageBoxNodeComplete(msg);
	}

	private void GetAllEquipmentsData(RecipeGroupInfo equipmentGroup)
	{
		totalDatas.Clear();
		foreach (EquipmentMenuSubType value in Enum.GetValues(typeof(EquipmentMenuSubType)))
		{
			totalDatas.Add(value, new List<string>());
		}
		foreach (string recipeId in equipmentGroup.RecipeIds)
		{
			if (DolocAPI.QueryEquipment(recipeId, out var proto) && proto.Display)
			{
				switch (proto.MenuType)
				{
				case EquipmentType.FARM:
					totalDatas[EquipmentMenuSubType.FARM].Add(proto.Id);
					break;
				case EquipmentType.LIFE:
					totalDatas[EquipmentMenuSubType.LIFE].Add(proto.Id);
					break;
				case EquipmentType.ANIMAL:
					totalDatas[EquipmentMenuSubType.ANIMAL].Add(proto.Id);
					break;
				case EquipmentType.INDUSTRY:
					totalDatas[EquipmentMenuSubType.INDUSTRY].Add(proto.Id);
					break;
				}
				if (DolocAPI.archiveHandle.farmData.recipeManager.CheckRecipeUnlocked(recipeId))
				{
					totalDatas[EquipmentMenuSubType.UNLOCK].Add(proto.Id);
				}
				totalDatas[EquipmentMenuSubType.ALL].Add(proto.Id);
			}
		}
	}

	private IEnumerable<string> GetCurrentDataList()
	{
		switch (CurrentEquipmentType)
		{
		case EquipmentMenuSubType.ALL:
		case EquipmentMenuSubType.FARM:
		case EquipmentMenuSubType.INDUSTRY:
		case EquipmentMenuSubType.LIFE:
		case EquipmentMenuSubType.ANIMAL:
		case EquipmentMenuSubType.UNLOCK:
			return totalDatas[CurrentEquipmentType];
		case EquipmentMenuSubType.COLLECT:
			return CollectEquipments.Where(delegate(string x)
			{
				DolocAPI.QueryEquipment(x, out var proto);
				return proto.Display;
			});
		default:
			return null;
		}
	}

	private void RefreshCurrentDataList()
	{
		switch (_equipmentSortMode)
		{
		case EquipmentSortMode.DEFAULT:
			DefaultSort();
			break;
		case EquipmentSortMode.UNLOCK:
			UnlockSort();
			break;
		}
	}

	private void Sort()
	{
		_equipmentSortMode = _equipmentSortMode.Next();
		RefreshCurrentDataList();
		base.panel.Select(GetDataIndex(_currentEquipmentName));
		base.panel.RefreshView();
		ShowSortInfo();
	}

	private void DefaultSort()
	{
		List<string> list = GetCurrentDataList().ToList();
		currentEquipments = new List<string>(list.OrderBy(DolocAPI.GetItemSortingOrder));
		foreach (string item in list)
		{
			DolocAPI.QueryEquipment(item, out var proto);
			if (!DolocAPI.archiveHandle.IsEquipmentUnlocked(item) && !proto.ShowCompleteInfo)
			{
				currentEquipments.Remove(item);
				currentEquipments.Add(item);
			}
		}
	}

	private void UnlockSort()
	{
		List<string> list = GetCurrentDataList().ToList();
		currentEquipments = new List<string>(GetCurrentDataList().OrderBy(DolocAPI.GetItemSortingOrder));
		foreach (string item in totalDatas[EquipmentMenuSubType.UNLOCK])
		{
			if (currentEquipments.Contains(item))
			{
				currentEquipments.Remove(item);
				currentEquipments.Insert(0, item);
			}
		}
		foreach (string item2 in list)
		{
			DolocAPI.QueryEquipment(item2, out var proto);
			if (!DolocAPI.archiveHandle.IsEquipmentUnlocked(item2) && !proto.ShowCompleteInfo)
			{
				currentEquipments.Remove(item2);
				currentEquipments.Add(item2);
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		base.panel.menuUI.FireClick(_currentTypeIndex);
		RefreshTip();
	}

	protected override void Hide()
	{
		base.Hide();
		_anim?.Kill();
	}

	public override void OnResume()
	{
		string currentEquipmentName = _currentEquipmentName;
		base.OnResume();
		base.panel.Select(GetDataIndex(currentEquipmentName));
		_hideOnPause = true;
	}

	private bool GetJumpNodeInfo(string equipmentName, out string techId, out string nodeName)
	{
		techId = string.Empty;
		nodeName = string.Empty;
		if (DolocAPI.archiveHandle.IsEquipmentUnlocked(equipmentName))
		{
			return false;
		}
		TreeGraph<TechNodeProto>[] allTreeGraphs = DolocAPI.assets.techTrees.AllTreeGraphs;
		foreach (TreeGraph<TechNodeProto> treeGraph in allTreeGraphs)
		{
			TreeGraphNode<TechNodeProto>[] nodes = treeGraph.nodes;
			for (int j = 0; j < nodes.Length; j++)
			{
				TechNodeProto data = nodes[j].data;
				string[] equipments = data.equipments;
				foreach (string text in equipments)
				{
					if (equipmentName == text)
					{
						techId = treeGraph.id;
						nodeName = data.id;
						return true;
					}
				}
			}
		}
		return false;
	}

	private int GetDataIndex(string equipmentName)
	{
		return currentEquipments.IndexOf(equipmentName);
	}

	protected override void RefreshTip()
	{
		List<string> list = new List<string>
		{
			base.staticTexts.UiTipSort,
			base.staticTexts.UiTipCollection
		};
		if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
		{
			list.Add(base.staticTexts.UiTipMakeAll);
			list.Add(base.staticTexts.UiTipMakeOne);
		}
		base.panel.operationTip.SetTextKey(list.ToArray());
	}
}
