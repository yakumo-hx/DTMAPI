using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;

namespace DolocTown.UI;

public class SettingUIItemManager
{
	private Transform content;

	private Dictionary<UserSettingType, GameObject> items = new Dictionary<UserSettingType, GameObject>();

	private Dictionary<UserSettingType, ISettingUiItem> uiCmps = new Dictionary<UserSettingType, ISettingUiItem>();

	private UserSettings userSettings;

	private GameObject textTpl => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SETTING_TITLE);

	private GameObject toggleTpl => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SETTING_TOGGLE);

	private GameObject sliderTpl => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SETTING_SLIDER);

	private GameObject optionTpl => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SETTING_OPTION);

	private GameObject rebindTpl => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_SETTING_REBIND_ACTION);

	public Dictionary<string, HashSet<ISettingUiItem>> uiCmpsByGroup { get; private set; } = new Dictionary<string, HashSet<ISettingUiItem>>();


	public List<ISettingUiItem> selectableCmps { get; private set; } = new List<ISettingUiItem>();


	public List<ISettingUiItem> allCmps { get; private set; } = new List<ISettingUiItem>();


	private TbUserSetting TbSetting => DolocConfig.Tables.TbUserSetting;

	public SettingUIItemManager(Transform content)
	{
		this.content = content;
	}

	private ISettingUiItem RenderUiItem(UserSettingInfo itemData)
	{
		GameObject gameObject = null;
		ISettingUiItem uiCmp = null;
		SettingComponentBase component = itemData.Component;
		if (!(component is EmptySettingComponent))
		{
			if (!(component is ToggleSettingComponent))
			{
				if (!(component is SliderSettingComponent cmpData))
				{
					if (!(component is OptionSettingComponent cmpData2))
					{
						if (component is RebindActionSettingComponent cmpData3)
						{
							gameObject = GetRebindActionItem(itemData, cmpData3, out uiCmp);
						}
					}
					else
					{
						gameObject = GetOptionItem(itemData, cmpData2, out uiCmp);
					}
				}
				else
				{
					gameObject = GetSliderItem(itemData, cmpData, out uiCmp);
				}
			}
			else
			{
				gameObject = GetToggleItem(itemData, out uiCmp);
			}
		}
		else
		{
			gameObject = GetTextItem(itemData, out uiCmp);
		}
		if (gameObject == null)
		{
			Debug.LogError($"对于{component.GetType()}没有对应的创建方法");
		}
		if (uiCmp == null)
		{
			Debug.LogError("没有获取到对应的ui组件");
		}
		else
		{
			uiCmp.config = itemData;
			allCmps.Add(uiCmp);
		}
		if (gameObject != null)
		{
			gameObject.SetActive(value: true);
		}
		return uiCmp;
	}

	private GameObject GetTextItem(UserSettingInfo itemData, out ISettingUiItem uiCmp)
	{
		UserSettingType id = itemData.Id;
		if (!items.TryGetValue(id, out var value))
		{
			value = Object.Instantiate(textTpl, content);
			items[id] = value;
			uiCmp = value.GetComponent<SettingItemUI>();
			((SettingItemUI)uiCmp).Init();
			uiCmps[id] = uiCmp;
		}
		uiCmp = uiCmps[id];
		uiCmp.title = itemData.Title;
		return value;
	}

	private GameObject GetToggleItem(UserSettingInfo itemData, out ISettingUiItem uiCmp)
	{
		UserSettingType id = itemData.Id;
		if (!items.TryGetValue(id, out var value))
		{
			value = Object.Instantiate(toggleTpl, content);
			items[id] = value;
			uiCmp = value.GetComponent<ToggleItemUI>();
			((ToggleItemUI)uiCmp).Init();
			uiCmps[id] = uiCmp;
		}
		uiCmp = uiCmps[id];
		uiCmp.title = itemData.Title;
		((ToggleItemUI)uiCmp).InitValue(userSettings.GetOrDefault<bool>(id), ((ToggleSettingComponent)itemData.Component).ShowSplitLine);
		selectableCmps.Add((ToggleItemUI)uiCmp);
		return value;
	}

	private GameObject GetSliderItem(UserSettingInfo itemData, SliderSettingComponent cmpData, out ISettingUiItem uiCmp)
	{
		UserSettingType id = itemData.Id;
		if (!items.TryGetValue(id, out var value))
		{
			value = Object.Instantiate(sliderTpl, content);
			items[id] = value;
			uiCmp = value.GetComponent<SliderItemUI>();
			((SliderItemUI)uiCmp).Init();
			uiCmps[id] = uiCmp;
		}
		uiCmp = uiCmps[id];
		uiCmp.title = itemData.Title;
		((SliderItemUI)uiCmp).InitValue(userSettings.GetOrDefault<int>(id), cmpData.MinValue, cmpData.MaxValue, cmpData.Scale);
		selectableCmps.Add((SliderItemUI)uiCmp);
		return value;
	}

	private GameObject GetOptionItem(UserSettingInfo itemData, OptionSettingComponent cmpData, out ISettingUiItem uiCmp)
	{
		UserSettingType id = itemData.Id;
		if (!items.TryGetValue(id, out var value))
		{
			value = Object.Instantiate(optionTpl, content);
			items[id] = value;
			uiCmp = value.GetComponent<OptionItemUI>();
			((OptionItemUI)uiCmp).Init();
			uiCmps[id] = uiCmp;
		}
		uiCmp = uiCmps[id];
		uiCmp.title = itemData.Title;
		string orDefault = userSettings.GetOrDefault<string>(id);
		int count = cmpData.OptionLables.Count;
		int currentIndex = 0;
		for (int i = 0; i < count; i++)
		{
			if (!(cmpData.OptionValues[i] != orDefault))
			{
				currentIndex = i;
				break;
			}
		}
		((OptionItemUI)uiCmp).InitValue(currentIndex, count, (int idx) => GetLabelText(idx, cmpData), (int idx) => cmpData.OptionValues[idx]);
		selectableCmps.Add((OptionItemUI)uiCmp);
		return value;
	}

	private string GetLabelText(int index, OptionSettingComponent cmpData)
	{
		int count = cmpData.OptionLables.Count;
		index = Mathf.Clamp(index, 0, count - 1);
		string text = cmpData.OptionLables[index];
		if (cmpData.UseL10nKey)
		{
			return DolocConfig.GetL10nText(text);
		}
		return text;
	}

	public void Render(UserSettings userSettings)
	{
		selectableCmps.Clear();
		this.userSettings = userSettings;
		foreach (UserSettingInfo data in TbSetting.DataList)
		{
			RenderUiItem(data);
		}
		if (!uiCmpsByGroup.IsNullOrEmpty())
		{
			return;
		}
		foreach (ISettingUiItem allCmp in allCmps)
		{
			uiCmpsByGroup.TryAdd(allCmp.config.Group, new HashSet<ISettingUiItem>());
			uiCmpsByGroup[allCmp.config.Group].Add(allCmp);
		}
	}

	private GameObject GetRebindActionItem(UserSettingInfo itemData, RebindActionSettingComponent cmpData, out ISettingUiItem uiCmp)
	{
		RebindActionUI rebindActionUI = null;
		UserSettingType id = itemData.Id;
		if (!items.TryGetValue(id, out var value))
		{
			value = Object.Instantiate(rebindTpl, content);
			items[id] = value;
			rebindActionUI = value.GetComponent<RebindActionUI>();
			rebindActionUI.config = itemData;
			rebindActionUI.Init();
			uiCmps[id] = rebindActionUI;
		}
		uiCmp = uiCmps[id];
		uiCmp.title = itemData.Title;
		selectableCmps.Add((RebindActionUI)uiCmp);
		return value;
	}

	public bool GetItemCmp<T>(UserSettingType id, out T item) where T : class, ISettingUiItem
	{
		item = null;
		if (!uiCmps.TryGetValue(id, out var value))
		{
			if (uiCmps.Count == 0)
			{
				Debug.LogError("没有初始化SettingPanel的数据");
			}
			Debug.LogError($"无效的id：{id}");
			return false;
		}
		item = value as T;
		if (item == null)
		{
			Debug.LogError($"无效的类型转换：{value.GetType()} to {typeof(T)}");
			return false;
		}
		return true;
	}

	public void Save()
	{
		DolocAPI.SaveUserSettings(userSettings);
	}

	public void Revert(UserSettings settings)
	{
		DolocAPI.RevertUserSettings(settings);
	}
}
