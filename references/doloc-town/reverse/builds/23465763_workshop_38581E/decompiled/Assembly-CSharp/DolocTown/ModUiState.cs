using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Settings;
using DolocTown.Config.UI;
using DolocTown.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class ModUiState : PageUiStateBase<ModPanel, ModData>
{
	private int currentTypeIndex;

	private ModInfo currentModInfo;

	private List<ModInfo> currentModList = new List<ModInfo>();

	private bool useModOnEnter;

	protected override int totalCapacity => currentModList.Count;

	protected override bool hideOnPause => false;

	private ModManager modManager => DolocAPI.modManager;

	private TbModMenu menuConfig => DolocConfig.Tables.TbModMenu;

	private ModViewer modViewer => base.panel.modViewer;

	private IScrollContentRect _contentRect => base.panel.contentRect;

	private ModMenuType currentMenyType => menuConfig.DataList[currentTypeIndex].Id;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	protected override ModData[] DataGetter(int start, int end)
	{
		List<ModData> list = new List<ModData>();
		int count = currentModList.Count;
		for (int i = start; i < Mathf.Min(count, end); i++)
		{
			list.Add(new ModData(currentModList[i]));
		}
		return list.ToArray();
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		base.panel.subMenu.Render(menuConfig.DataMap.Values.Select((ModMenuInfo x) => x.IconAsset.Asset).ToArray(), menuConfig.DataMap.Values.Select((ModMenuInfo x) => x.MenuTitle).ToArray());
		base.panel.onToggleSwitch = delegate(int index)
		{
			ToggleModEnabled(index + base.panel.offset);
		};
		base.panel.onMoveUp = delegate(int index)
		{
			MoveModUp(index + base.panel.offset);
		};
		base.panel.onMoveDown = delegate(int index)
		{
			MoveModDown(index + base.panel.offset);
		};
	}

	protected override void Register()
	{
		base.Register();
		useModOnEnter = DolocAPI.UseMods;
		modManager.ReloadMods();
		base.panel.onDataClick.AddListener(OnModClick);
		base.panel.onDataSelect.AddListener(OnModSelect);
		modViewer.switchButton.onClick.AddListener(ToggleCurrentMod);
		modViewer.moveUpButton.onClick.AddListener(MoveCurrentModUp);
		modViewer.moveDownButton.onClick.AddListener(MoveCurrentModDown);
		modViewer.uploadButton.onClick.AddListener(UploadMod);
		modViewer.workshopButton.onClick.AddListener(OpenWorkshop);
		modViewer.openLocalButton.onClick.AddListener(OpenLocalSpace);
		modViewer.workshopHomepageButton.onClick.AddListener(OpenWorkshop);
		modViewer.openLocalRootButton.onClick.AddListener(OpenLocalSpace);
		base.panel.subMenu.SetSelectCallbacks(OnMenuIconSelect);
	}

	protected override void Unregister()
	{
		base.Unregister();
		base.panel.onDataClick.RemoveListener(OnModClick);
		base.panel.onDataSelect.RemoveListener(OnModSelect);
		modViewer.switchButton.onClick.RemoveListener(ToggleCurrentMod);
		modViewer.moveUpButton.onClick.RemoveListener(MoveCurrentModUp);
		modViewer.moveDownButton.onClick.RemoveListener(MoveCurrentModDown);
		modViewer.uploadButton.onClick.RemoveListener(UploadMod);
		modViewer.workshopButton.onClick.RemoveListener(OpenWorkshop);
		modViewer.openLocalButton.onClick.RemoveListener(OpenLocalSpace);
		modViewer.workshopHomepageButton.onClick.RemoveListener(OpenWorkshop);
		modViewer.openLocalRootButton.onClick.RemoveListener(OpenLocalSpace);
		base.panel.subMenu.RemoveCallbacks();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (ContinuouslyPressLast(deltaTime, base.panel.PrevPage) || ContinuouslyPressNext(deltaTime, base.panel.NextPage))
		{
			return;
		}
		if (userInput.BaseIsCancelPressed)
		{
			ModSlot slot = base.panel.GetSlot(base.panel.selectedIndex);
			if (totalCapacity == 0 || slot == null || slot.IsSelected)
			{
				gameController.PopState();
			}
			else
			{
				slot.Select();
			}
		}
		else if (userInput.BasePageUpPressed)
		{
			base.panel.subMenu.FireClickLeft();
		}
		else if (userInput.BasePageDownPressed)
		{
			base.panel.subMenu.FireClickRight();
		}
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
	}

	private void TryClickButton(ModFunctionButton button)
	{
		if (button.gameObject.activeSelf)
		{
			button.FireClick();
		}
	}

	protected override void Show()
	{
		base.Show();
		if (!modManager.HasEnabledMods)
		{
			currentTypeIndex = 2;
		}
		base.panel.subMenu.FireClick(currentTypeIndex, fireSelect: true);
		RefreshNavigation();
	}

	protected override void Hide()
	{
		base.Hide();
		currentModInfo = null;
		currentModList.Clear();
		DolocAPI.DelayFrame(delegate
		{
			DolocAPI.ShowPendingBox(base.staticTexts.UiModReloading);
			DolocAPI.Delay(0.3f, delegate
			{
				modManager.ReloadMods();
				DolocAPI.dataPersistenceManager.SaveModManager(modManager);
				if (DolocAPI.UseMods || useModOnEnter)
				{
					DolocConfig.Reload();
					DolocAPI.SwitchLanguage(DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.LANGUAGE_TEXT));
				}
				DolocAPI.HidePendingBox();
				useModOnEnter = false;
			});
		});
	}

	private void OnModClick(int index)
	{
		if (DolocButtonComponent.latestClickType != ClickType.Mouse)
		{
			modViewer.switchButton.Select();
		}
	}

	private void OnModSelect(int index)
	{
		currentModInfo = null;
		if (index < 0 || index >= totalCapacity)
		{
			return;
		}
		currentModInfo = currentModList[index];
		ModSlot[] slots = base.panel.slots;
		foreach (ModSlot modSlot in slots)
		{
			if (modSlot.index != index - base.panel.offset)
			{
				modSlot.highLighted = false;
			}
		}
		RefreshCurrentModViewer();
		RefreshCurrentModUploadPlan();
		DolocAPI.UIRaiseRoll();
		RefreshNavigation();
	}

	private void RefreshCurrentModViewer()
	{
		if (currentModInfo != null)
		{
			modManager.TryGetResolvedLocalModUploadPlan(currentModInfo, out var plan);
			modViewer.Render(new ModData(currentModInfo, plan));
		}
	}

	private void RefreshCurrentModUploadPlan()
	{
		ModInfo modInfo = currentModInfo;
		if (modInfo == null || modInfo.source != 0)
		{
			return;
		}
		ulong workshopId = modInfo.workshopId;
		modManager.ResolveLocalModUploadPlan(modInfo, delegate(WorkshopUploadPlan plan)
		{
			if (currentModInfo == modInfo && modInfo.workshopId == workshopId)
			{
				modViewer.Render(new ModData(modInfo, plan));
			}
		});
	}

	private void OnMenuIconSelect(int index)
	{
		currentTypeIndex = index;
		RefreshView(refreshData: true);
		DolocAPI.DelayFrame(delegate
		{
			base.panel.Select(0);
		});
		base.panel.SetTitle(menuConfig.DataList[currentTypeIndex].Title);
		DolocAPI.UIRaisePage();
		RefreshNavigation();
	}

	private void RefreshData()
	{
		currentModList.Clear();
		ModInfo[] array = modManager.SortedModInfos.ToArray();
		if (!array.IsNullOrEmpty())
		{
			currentModList = currentMenyType switch
			{
				ModMenuType.Enabled => array.Where((ModInfo x) => x.enabled).ToList(), 
				ModMenuType.Disabled => array.Where((ModInfo x) => !x.enabled).ToList(), 
				_ => array.ToList(), 
			};
		}
		base.panel.SetTotalCapacity(totalCapacity);
	}

	private void RefreshView(bool refreshData)
	{
		if (refreshData)
		{
			RefreshData();
		}
		base.panel.RefreshView();
		base.panel.SetEmpty(totalCapacity == 0);
		RefreshNavigation();
	}

	private void ToggleCurrentMod(int _)
	{
		ToggleModEnabled(base.panel.selectedIndex);
		DolocAPI.DelayFrame(delegate
		{
			if (totalCapacity == 0)
			{
				modViewer.workshopHomepageButton.Select();
			}
			else
			{
				modViewer.switchButton.Select();
			}
		}, 2);
	}

	private void MoveCurrentModUp(int _)
	{
		MoveModUp(base.panel.selectedIndex);
		DolocAPI.DelayFrame(delegate
		{
			if (totalCapacity == 0)
			{
				modViewer.workshopHomepageButton.Select();
			}
			else if (modViewer.moveUpButton.interactable)
			{
				modViewer.moveUpButton.Select();
			}
			else
			{
				modViewer.switchButton.Select();
			}
		}, 2);
	}

	private void MoveCurrentModDown(int _)
	{
		MoveModDown(base.panel.selectedIndex);
		DolocAPI.DelayFrame(delegate
		{
			if (totalCapacity == 0)
			{
				modViewer.workshopHomepageButton.Select();
			}
			else if (modViewer.moveDownButton.interactable)
			{
				modViewer.moveDownButton.Select();
			}
			else
			{
				modViewer.switchButton.Select();
			}
		}, 2);
	}

	private void ToggleModEnabled(int index)
	{
		ModInfo modInfo = currentModList[index];
		modManager.ToggleModEnabled(modInfo);
		RefreshView(refreshData: true);
		SelectMod(modInfo);
	}

	private void MoveModUp(int index)
	{
		ModInfo modInfo = currentModList[index];
		modManager.MovePrev(modInfo);
		RefreshView(refreshData: true);
		SelectMod(modInfo);
	}

	private void MoveModDown(int index)
	{
		ModInfo modInfo = currentModList[index];
		modManager.MoveNext(modInfo);
		RefreshView(refreshData: true);
		SelectMod(modInfo);
	}

	private void SelectMod(ModInfo modInfo)
	{
		int i;
		for (i = 0; i < currentModList.Count; i++)
		{
			if (currentModList[i].id == modInfo.id)
			{
				DolocAPI.DelayFrame(delegate
				{
					base.panel.Select(i);
				});
				break;
			}
		}
	}

	private void RefreshNavigation()
	{
		DolocAPI.DelayFrame(delegate
		{
			bool flag = totalCapacity == 0;
			base.panel.RefreshNavigation(flag ? null : base.panel.GetSlot(base.panel.selectedIndex));
			if (flag)
			{
				modViewer.workshopHomepageButton.Select();
			}
		}, 3);
	}

	private void UploadMod(int index)
	{
		ModInfo modInfo = currentModInfo;
		if (modInfo == null)
		{
			return;
		}
		modManager.ResolveLocalModUploadPlan(modInfo, delegate(WorkshopUploadPlan plan)
		{
			DolocAPI.ShowQuestionBox(DolocUtils.Format((plan.mode == WorkshopUploadMode.Update) ? base.staticTexts.UiModConfirmUpdate : base.staticTexts.UiModConfirmUpload, modInfo.title.Colored(DolocUiColor.EYECATCHCOLOR_CYAN)), delegate
			{
				Debug.Log($"{plan.mode} local mod {modInfo.title}");
				DolocAPI.ShowPendingBox((plan.mode == WorkshopUploadMode.Update) ? base.staticTexts.UiModUpdating : base.staticTexts.UiModUploading);
				modManager.UploadLocalMod(modInfo, plan, delegate(WorkshopUploadResult result)
				{
					DolocAPI.HidePendingBox();
					ShowUploadResultMessage(result);
					if (currentModInfo == modInfo)
					{
						RefreshCurrentModViewer();
					}
				});
			});
		});
	}

	private void ShowUploadResultMessage(WorkshopUploadResult result)
	{
		if (result.mode == WorkshopUploadMode.Update)
		{
			if (result.success)
			{
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.UiModUpdateSuccess);
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiModUpdateFailed);
			}
		}
		else if (result.success)
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.UiModUploadSuccess);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiModUploadFailed);
		}
	}

	private void OpenWorkshop(int index)
	{
		SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/app/{SteamUtils.GetAppID()}/workshop/");
	}

	private void OpenLocalSpace(int index)
	{
		Application.OpenURL((currentModInfo == null) ? ("file://" + modManager.ModsRoot) : ("file://" + currentModInfo.rootPath));
	}
}
