using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Animal;
using DolocTown.Config.Archives;
using DolocTown.Config.Fishing;
using DolocTown.Config.Item;
using DolocTown.Config.Monster;
using DolocTown.Config.NPC;
using DolocTown.Config.Resource;
using DolocTown.Config.UI;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class CollectionBookUiState : DolocUiState<CollectionBookPanel>
{
	private int itemTypeIndex;

	private int archiveTypeIndex;

	private string _currentItem;

	private List<string> itemList = new List<string>();

	private List<string> monsterList = new List<string>();

	private List<string> npcList = new List<string>();

	private List<DocumentData> documentList = new List<DocumentData>();

	private List<string> creatureList = new List<string>();

	private List<string> resourceList = new List<string>();

	private int gotoLabelIndex = -1;

	private IScrollContentRect _contentRect;

	private ItemCatalogPanel itemPanel => base.panel.itemCatalogPanel;

	private CreatureCatalogPanel creaturePanel => base.panel.creatureCatalogPanel;

	private MonsterCatalogPanel monsterPanel => base.panel.monsterCatalogPanel;

	private ResourceCatalogPanel resourcePanel => base.panel.resourceCatalogPanel;

	private NpcCatalogPanel npcPanel => base.panel.npcCatalogPanel;

	private ArchiveCatalogPanel archivePanel => base.panel.archiveCatalogPanel;

	private MapPanel mapPanel => base.panel.mapPanel;

	public override bool ShowPauseTip => false;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private CollectionManager CollectionManager => DolocAPI.archiveHandle.farmData.collectionManager;

	private DocumentManager DocumentManager => DolocAPI.archiveHandle.cityData.documentManager;

	private TbCompendiumMenu MenuConfig => DolocConfig.Tables.TbCompendiumMenu;

	private ItemMainTypeInfo[] ItemCollectionLabels => DolocAPI.GlobalParameter.ItemCollectionLabels_Ref;

	private CompendiumLabel labelIndex => (CompendiumLabel)base.panel.subMenu.selectIndex;

	private int joystickMoveDir => 8;

	private int mapMoveDir => 15;

	public bool OpenNpcArchiveWithId(string documentId)
	{
		if (!DocumentManager.characterDocMgr.GetDocumentUnlockState(documentId))
		{
			return false;
		}
		gotoLabelIndex = 5;
		archiveTypeIndex = 0;
		int index = -1;
		foreach (CharacterDocumentInfo data in DolocConfig.Tables.TbCharacterDocument.DataList)
		{
			if (DocumentManager.characterDocMgr.GetDocumentUnlockState(data.Id))
			{
				index++;
			}
			if (data.Id == documentId)
			{
				break;
			}
		}
		DolocAPI.DelayFrame(delegate
		{
			archivePanel.archiveListViewer.Select(index);
		});
		return true;
	}

	public bool HandleMapArgs(string mapId)
	{
		gotoLabelIndex = 6;
		mapPanel.SetFirstSelectedMap(mapId);
		DolocAPI.Broadcast(OperationEventType.OPEN_MAP_PANEL);
		return true;
	}

	public bool OpenMapWithMission(string missionId)
	{
		gotoLabelIndex = 6;
		mapPanel.SetFocusedMission(missionId);
		return true;
	}

	protected override void BeforeRegister()
	{
		List<bool> list = MenuConfig.DataList.Select((CompendiumMenuInfo info) => info.DefaultUnlock || CollectionManager.CheckCollectionFuncUnlocked(info.Id)).ToList();
		if (DocumentManager.characterDocMgr.unlockedDocuments.Count == 0 && !DocumentManager.isSynchronizeChipDoc && !DocumentManager.isSynchronizeChipDoc)
		{
			list[5] = false;
		}
		int num = list.FindIndex((bool x) => x);
		gotoLabelIndex = ((gotoLabelIndex < 0) ? num : gotoLabelIndex);
		base.panel.subMenu.Render(MenuConfig.DataList.Select((CompendiumMenuInfo x) => x.Title).ToArray(), list.ToArray());
	}

	protected override void Register()
	{
		base.panel.subMenu.SetClickCallbacks(ClickBookLabel);
		itemPanel.itemListViewer.SetSelectCallbacks(OnItemDataSelect);
		itemPanel.itemListViewer.SetClickCallbacks(OnItemDataClick);
		itemPanel.optionItem.OnValueChanged = delegate(int value)
		{
			itemTypeIndex = value;
			RefreshItemList();
			itemPanel.itemListViewer.Select(0);
		};
		itemPanel.optionItem.InitValue(itemTypeIndex, ItemCollectionLabels.Length, (int i) => ItemCollectionLabels[i].Title, (int i) => i);
		creaturePanel.creatureListViewer.SetSelectCallbacks(OnCreatureDataSelect);
		RefreshCreatureList();
		monsterPanel.monsterListViewer.SetSelectCallbacks(OnMonsterDataSelect);
		RefreshMonsterList();
		resourcePanel.resourceListViewer.SetSelectCallbacks(OnResourceDataSelect);
		RefreshResourceList();
		npcPanel.npcListViewer.SetSelectCallbacks(OnNpcDataSelect);
		RefreshNpcList();
		archivePanel.archiveListViewer.SetSelectCallbacks(OnArchiveDataSelect);
		InitArchiveOption();
		mapPanel.Register();
	}

	protected override void Unregister()
	{
		base.panel.subMenu.SetClickCallbacks(null);
		itemPanel.itemListViewer.RemoveCallbacks();
		monsterPanel.monsterListViewer.RemoveCallbacks();
		mapPanel.Unregister();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (labelIndex == CompendiumLabel.Map)
		{
			if (userInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
			{
				mapPanel.UpdateCursorPosition(userInput.MousePosition);
			}
			else
			{
				mapPanel.UpdateCursorPositionByDelta(userInput.BaseScrollDir * joystickMoveDir);
			}
			if (userInput.BaseMove.magnitude > 0f)
			{
				mapPanel.MapScrolling(userInput.BaseMove * mapMoveDir);
			}
			if (userInput.BaseIsConfirmPressed)
			{
				mapPanel.SwitchMapSize();
			}
			if (userInput.BaseMiscellaneousPressed)
			{
				mapPanel.MoveToAgentPosition();
			}
		}
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
		if (userInput.BasePageUpPressed)
		{
			LastLabel();
		}
		else if (userInput.BasePageDownPressed)
		{
			NextLabel();
		}
		else if (userInput.BaseIsNextPressed)
		{
			if (labelIndex == CompendiumLabel.Item)
			{
				itemPanel.optionItem.ClickNext();
			}
			else if (labelIndex == CompendiumLabel.Archive)
			{
				archivePanel.optionItem.ClickNext();
			}
			else if (labelIndex == CompendiumLabel.Map)
			{
				mapPanel.SelectNextMap();
			}
		}
		else if (userInput.BaseIsLastPressed)
		{
			if (labelIndex == CompendiumLabel.Item)
			{
				itemPanel.optionItem.ClickPrev();
			}
			else if (labelIndex == CompendiumLabel.Archive)
			{
				archivePanel.optionItem.ClickPrev();
			}
			else if (labelIndex == CompendiumLabel.Map)
			{
				mapPanel.SelectPrevMap();
			}
		}
		else if (userInput.GlobalToggleMap)
		{
			if (labelIndex == CompendiumLabel.Map && userInput.GlobalToggleMap && userInput.BaseNotFixedOperation)
			{
				DelayFrameToPopState();
			}
			else
			{
				base.panel.subMenu.LastLabel();
			}
		}
		else if (userInput.BaseIsCancelPressed || (userInput.GlobalToggleCollectionBook && userInput.BaseNotFixedOperation))
		{
			OnToggleCollectionBook();
		}
	}

	private void OnToggleCollectionBook()
	{
		if (labelIndex == CompendiumLabel.Item && !itemPanel.itemListViewer.isFocused)
		{
			itemPanel.itemListViewer.GetFocus();
			base.panel.operationTip.SetTextKey(base.staticTexts.UiTipSwitchClassifying);
		}
		else
		{
			DelayFrameToPopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		base.panel.Show();
		mapPanel.Hide();
		mapPanel.EnableDetectCursor(value: false);
		base.panel.operationTip.SetTextKey(base.staticTexts.UiTipSwitchClassifying);
		base.panel.subMenu.Select(gotoLabelIndex);
	}

	protected override void Hide()
	{
		base.Hide();
		gotoLabelIndex = -1;
		_currentItem = string.Empty;
		base.panel.Hide();
		mapPanel.ClearFocusedData();
		DolocAPI.HideItemBorder();
	}

	private void ClickBookLabel(int index)
	{
		itemPanel.SetVisible(value: false);
		creaturePanel.SetVisible(value: false);
		monsterPanel.SetVisible(value: false);
		resourcePanel.SetVisible(value: false);
		npcPanel.SetVisible(value: false);
		archivePanel.SetVisible(value: false);
		base.panel.operationTip.SetVisible(value: true);
		if (labelIndex != CompendiumLabel.Map || index != 6)
		{
			mapPanel.Hide();
			mapPanel.EnableDetectCursor(value: false);
		}
		DolocAPI.HideItemBorder();
		switch ((CompendiumLabel)index)
		{
		case CompendiumLabel.Item:
			itemPanel.SetVisible(value: true);
			itemPanel.itemListViewer.GetFocus();
			_contentRect = itemPanel;
			break;
		case CompendiumLabel.Creature:
			creaturePanel.SetVisible(value: true);
			creaturePanel.creatureListViewer.GetFocus();
			_contentRect = creaturePanel;
			break;
		case CompendiumLabel.Monster:
			monsterPanel.SetVisible(value: true);
			monsterPanel.monsterListViewer.GetFocus();
			_contentRect = monsterPanel;
			break;
		case CompendiumLabel.Resource:
			resourcePanel.SetVisible(value: true);
			resourcePanel.resourceListViewer.GetFocus();
			_contentRect = resourcePanel;
			break;
		case CompendiumLabel.Npc:
			npcPanel.SetVisible(value: true);
			npcPanel.npcListViewer.GetFocus();
			_contentRect = npcPanel;
			break;
		case CompendiumLabel.Archive:
			archivePanel.SetVisible(value: true);
			archivePanel.archiveListViewer.GetFocus();
			_contentRect = archivePanel;
			break;
		case CompendiumLabel.Map:
			base.panel.operationTip.SetVisible(value: false);
			if (!mapPanel.isRender)
			{
				mapPanel.Show();
				mapPanel.EnableDetectCursor(value: true);
				_contentRect = null;
			}
			break;
		}
	}

	private void NextLabel()
	{
		base.panel.subMenu.NextLabel();
	}

	private void LastLabel()
	{
		base.panel.subMenu.PrevLabel();
	}

	private void RefreshItemList()
	{
		itemList.Clear();
		List<ItemInfo> source = (from info in DolocConfig.Tables.TbItem.DataList
			where info.MainType.Id == ItemCollectionLabels[itemTypeIndex].Id && info.Viewable
			select info into x
			orderby DolocAPI.GetItemSortingOrder(x.Id)
			select x).ToList();
		itemList = source.Select((ItemInfo x) => x.Id).ToList();
		BookItemData[] datas = source.Select((ItemInfo x) => new BookItemData(x)).ToArray();
		itemPanel.itemListViewer.RefreshView(datas);
	}

	private void OnItemDataSelect(int index)
	{
		if (index < 0 || index >= itemList.Count)
		{
			return;
		}
		string text = itemList[index];
		if (!(text == _currentItem))
		{
			_currentItem = text;
			itemPanel.itemViewer.Render(new ItemDetailData(text));
			if (CollectionManager.ReadCollectionRecord(CompendiumLabel.Item, text))
			{
				RefreshItemList();
			}
		}
	}

	private void OnItemDataClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			ItemDetailData currentData = itemPanel.itemViewer.currentData;
			if (currentData.notEmpty && currentData.obtained && currentData.recipesData.Length != 0)
			{
				itemPanel.itemListViewer.LoseFocus();
				itemPanel.itemViewer.itemRecipeListViewer.SelectFirst();
				base.panel.operationTip.SetTextKey(new string[2]
				{
					base.staticTexts.UiTipQuit,
					base.staticTexts.UiTipSwitchClassifying
				});
			}
		}
	}

	private void RefreshMonsterList()
	{
		monsterList.Clear();
		List<BookMonsterData> list = new List<BookMonsterData>();
		foreach (MonsterDocumentInfo data in DolocConfig.Tables.TbMonsterDocument.DataList)
		{
			if (new BookMonsterData(data.Id).display)
			{
				list.Add(new BookMonsterData(data.Id));
				monsterList.Add(data.Id);
			}
		}
		monsterPanel.monsterListViewer.RefreshView(list.ToArray());
	}

	private void OnMonsterDataSelect(int index)
	{
		if (index >= 0 && index < monsterList.Count)
		{
			string text = monsterList[index];
			monsterPanel.monsterViewer.Render(new MonsterDetailData(text));
			if (CollectionManager.ReadCollectionRecord(CompendiumLabel.Monster, text))
			{
				RefreshMonsterList();
			}
		}
	}

	private void RefreshResourceList()
	{
		resourceList.Clear();
		List<BookResourceData> list = new List<BookResourceData>();
		foreach (ResourceDocumentInfo data in DolocConfig.Tables.TbResourceDocument.DataList)
		{
			list.Add(new BookResourceData(data.Id));
			resourceList.Add(data.Id);
		}
		resourcePanel.resourceListViewer.RefreshView(list.ToArray());
	}

	private void OnResourceDataSelect(int index)
	{
		if (index >= 0 && index < resourceList.Count)
		{
			string text = resourceList[index];
			resourcePanel.RenderViewer(new ResourceDetailData(text));
			if (CollectionManager.ReadCollectionRecord(CompendiumLabel.Resource, text))
			{
				RefreshResourceList();
			}
		}
	}

	private void RefreshNpcList()
	{
		npcList.Clear();
		List<BookNpcData> list = new List<BookNpcData>();
		foreach (NpcDocumentInfo data in DolocConfig.Tables.TbNpcDocument.DataList)
		{
			list.Add(new BookNpcData(data.Id));
			npcList.Add(data.Id);
		}
		npcPanel.npcListViewer.RefreshView(list.ToArray());
	}

	private void OnNpcDataSelect(int index)
	{
		if (index >= 0 && index < npcList.Count)
		{
			string text = npcList[index];
			npcPanel.npcViewer.Render(new NpcDetailData(text));
			if (CollectionManager.ReadCollectionRecord(CompendiumLabel.Npc, text))
			{
				RefreshNpcList();
			}
		}
	}

	private void InitArchiveOption()
	{
		archivePanel.optionItem.Title = base.staticTexts.CollectionPanelDocumentLabel;
		archivePanel.optionItem.OnValueChanged = delegate(int value)
		{
			archiveTypeIndex = value;
			RefreshArchiveList();
			archivePanel.archiveListViewer.Select(0);
		};
		int num = 1;
		if (DocumentManager.isSynchronizeChipDoc)
		{
			num++;
		}
		if (DocumentManager.isSynchronizePlantDoc)
		{
			num++;
		}
		archivePanel.optionItem.InitValue(archiveTypeIndex, num, (int index) => index switch
		{
			0 => base.staticTexts.NpcDocumentTitle, 
			1 => DocumentManager.isSynchronizeChipDoc ? base.staticTexts.ChipDocumentTitle : (DocumentManager.isSynchronizePlantDoc ? base.staticTexts.PlantDocumentTitle : string.Empty), 
			2 => DocumentManager.isSynchronizePlantDoc ? base.staticTexts.PlantDocumentTitle : string.Empty, 
			_ => string.Empty, 
		}, (int i) => i);
	}

	private void RefreshArchiveList()
	{
		documentList.Clear();
		if (archivePanel.optionItem.OptionLabel == base.staticTexts.NpcDocumentTitle)
		{
			foreach (CharacterDocumentInfo data in DolocConfig.Tables.TbCharacterDocument.DataList)
			{
				if (DocumentManager.characterDocMgr.GetDocumentUnlockState(data.Id))
				{
					documentList.Add(new DocumentData(data.Title, data.Subhead, data.Content));
				}
			}
		}
		if (archivePanel.optionItem.OptionLabel == base.staticTexts.ChipDocumentTitle)
		{
			foreach (string item in DocumentManager.chipDocMgr.unlockedOrder)
			{
				ChipDocumentInfo orDefault = DolocConfig.Tables.TbChipDocument.GetOrDefault(item);
				documentList.Add(new DocumentData(orDefault.Title, orDefault.Author, orDefault.Content));
			}
		}
		if (archivePanel.optionItem.OptionLabel == base.staticTexts.PlantDocumentTitle)
		{
			foreach (string item2 in DocumentManager.plantDocMgr.unlockedOrder)
			{
				PlantDocumentInfo orDefault2 = DolocConfig.Tables.TbPlantDocument.GetOrDefault(item2);
				documentList.Add(new DocumentData(orDefault2.Title, orDefault2.Author, orDefault2.Content));
			}
		}
		archivePanel.archiveListViewer.RefreshView(documentList.ToArray());
		archivePanel.archiveViewer.SetEmpty(documentList.Count == 0);
	}

	private void OnArchiveDataSelect(int index)
	{
		if (index >= 0 && index < documentList.Count)
		{
			archivePanel.archiveViewer.Render(documentList[index]);
		}
	}

	private void RefreshCreatureList()
	{
		creatureList.Clear();
		List<BookCreatureData> list = new List<BookCreatureData>();
		foreach (AnimalDocumentInfo data in DolocConfig.Tables.TbAnimalDocument.DataList)
		{
			BookCreatureData item = new BookCreatureData(data.Id);
			if (item.notEmpty)
			{
				list.Add(item);
				creatureList.Add(data.Id);
			}
		}
		foreach (FishDocumentInfo data2 in DolocConfig.Tables.TbFishDocument.DataList)
		{
			BookCreatureData item2 = new BookCreatureData(data2.Id);
			if (item2.notEmpty)
			{
				list.Add(item2);
				creatureList.Add(data2.Id);
			}
		}
		creaturePanel.creatureListViewer.RefreshView(list.ToArray());
	}

	private void OnCreatureDataSelect(int index)
	{
		if (index >= 0 && index < creatureList.Count)
		{
			string text = creatureList[index];
			creaturePanel.creatureViewer.Render(new CreatureDetailData(text));
			if (CollectionManager.ReadCollectionRecord(CompendiumLabel.Creature, text))
			{
				RefreshCreatureList();
			}
		}
	}
}
