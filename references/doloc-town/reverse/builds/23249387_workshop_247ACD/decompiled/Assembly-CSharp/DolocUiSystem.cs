using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.UI;
using DolocTown.UI;
using UnityEngine;

public class DolocUiSystem : MonoBehaviour
{
	private readonly Dictionary<string, UiGroup> groups = new Dictionary<string, UiGroup>();

	private readonly Dictionary<string, DolocUiEntity> entites = new Dictionary<string, DolocUiEntity>();

	private readonly Dictionary<string, HashSet<object>> refHolders = new Dictionary<string, HashSet<object>>();

	private GameObject canvasRootObject;

	public RectTransform rectTransform { get; private set; }

	private UiRendererPoolManagerBase canvasUiPoolManager { get; set; }

	private UiRendererPoolManagerBase sceneUiPoolManager { get; set; }

	public QuickInventoryPanel inventoryQuick { get; private set; }

	public ExchangeInventory inventoryMouse { get; private set; }

	public GameTimeTip gameTimeTip { get; private set; }

	public BasicTipGroup basicTip { get; private set; }

	public MoneyTip MoneyTip => basicTip.MoneyTip;

	public AgentStatusTip agentStatusBar => basicTip.AgentStatusBar;

	public MissionTipManager MissionTips => basicTip.MissionTips;

	public BuffTip BuffTip => basicTip.BuffTip;

	public OperationGuidanceManager GuidanceTips => basicTip.guidanceTips;

	public MessageBoxManager messageBoxManager { get; private set; }

	public OperationTipInSceneManager sceneOperationTipManager { get; private set; }

	public OperationTipMultiManager sceneOperationTipMultiManager { get; private set; }

	public DialogueHistoryTip dialogueHistoryTip { get; private set; }

	public SceneBoxGroup sceneBoxGroup { get; private set; }

	public void Init()
	{
		canvasRootObject = new GameObject("UICanvas");
		rectTransform = GetComponent<RectTransform>();
		InitCanvasGroups();
		canvasUiPoolManager = GetEntity<CanvasUiRendererPoolManager>();
		sceneUiPoolManager = GetEntity<SceneUiRendererPoolManager>();
		InitMain();
		InitOthers();
	}

	private void InitCanvasGroups()
	{
		foreach (UIEntityInfo data in DolocConfig.Tables.TbUIEntity.DataList)
		{
			if (data.Preload)
			{
				string id = data.Id;
				CreateUIEntityInternal(id);
			}
		}
	}

	public T GetEntity<T>(object refHolder = null) where T : DolocUiEntity
	{
		string key = typeof(T).Name;
		DolocUiEntity value;
		T val = ((!entites.TryGetValue(key, out value) || value is DolocUIPanel { isWidget: not false }) ? (CreateUIEntityInternal(key) as T) : ((T)value));
		if (val != null && refHolder != null)
		{
			refHolders.TryAdd(key, new HashSet<object>());
			refHolders[key].Add(refHolder);
		}
		return val;
	}

	public void TryDestroyEntity<T>(object refHolder = null) where T : DolocUiEntity
	{
		string key = typeof(T).Name;
		if (refHolder != null && refHolders.TryGetValue(key, out var value))
		{
			value.Remove(refHolder);
		}
		if (refHolders.ContainsKey(key) && refHolders[key].Count != 0)
		{
			return;
		}
		refHolders.Remove(key);
		if (entites.Remove(key, out var value2))
		{
			if (groups.TryGetValue(value2.GroupName, out var value3))
			{
				value3.RemoveEntity(value2);
			}
			else
			{
				Object.Destroy(value2.gameObject);
			}
		}
	}

	private DolocUiEntity CreateUIEntityInternal(string key)
	{
		UIEntityInfo uIEntityInfo = DolocConfig.Tables.TbUIEntity.GetOrDefault(key);
		GameObject asset;
		if (uIEntityInfo == null)
		{
			Debug.LogWarning("未找到UIEntity<" + key + ">对应的配置！");
			asset = DolocAPI.GetAsset<GameObject>(key);
			uIEntityInfo = DolocAPI.GlobalParameter.DefaultUiEntityInfo_Ref;
		}
		else
		{
			asset = uIEntityInfo.PrefabAsset.Asset;
		}
		if (asset == null)
		{
			Debug.LogError("UIEntity<" + key + ">预制体加载失败！");
			return null;
		}
		UIEntityGroupInfo group_Ref = uIEntityInfo.Group_Ref;
		string group = uIEntityInfo.Group;
		if (!groups.TryGetValue(group, out var value))
		{
			value = Object.Instantiate(DolocAPI.GlobalParameter.UiGroupAsset.Asset, canvasRootObject.transform).GetComponent<UiGroup>();
			value.Init();
			value.gameObject.GetComponent<Canvas>().sortingOrder = group_Ref.Order;
			value.name = group;
			groups[group] = value;
		}
		GameObject obj = Object.Instantiate(asset, value.container);
		DolocUiEntity dolocUiEntity = null;
		DolocUiEntity[] components = obj.GetComponents<DolocUiEntity>();
		foreach (DolocUiEntity dolocUiEntity2 in components)
		{
			if (dolocUiEntity2.GetType().Name == key)
			{
				dolocUiEntity = dolocUiEntity2;
				break;
			}
		}
		if (dolocUiEntity != null)
		{
			dolocUiEntity.SetVisible(value: false);
			dolocUiEntity.Init();
			entites[dolocUiEntity.id] = dolocUiEntity;
			value.AddEntity(dolocUiEntity);
		}
		else
		{
			Debug.LogError("预加载ui<" + key + ">失败");
		}
		return dolocUiEntity;
	}

	public void SetSceneUIVisible(bool value)
	{
		foreach (KeyValuePair<string, UiGroup> group in groups)
		{
			if (DolocConfig.Tables.TbUIEntityGroup.GetOrDefault(group.Key).InScene)
			{
				group.Value.gameObject.SetActive(value);
			}
		}
	}

	public void SetPhotoMode(bool value)
	{
		foreach (KeyValuePair<string, UiGroup> group in groups)
		{
			group.Deconstruct(out var key, out var value2);
			string key2 = key;
			UiGroup uiGroup = value2;
			UIEntityGroupInfo orDefault = DolocConfig.Tables.TbUIEntityGroup.GetOrDefault(key2);
			if (value)
			{
				uiGroup.gameObject.SetActive(orDefault.ShowInPhotoState);
			}
			else
			{
				uiGroup.gameObject.SetActive(value: true);
			}
		}
	}

	public void SetGroupsVisible(bool value)
	{
		foreach (UiGroup value2 in groups.Values)
		{
			value2.gameObject.SetActive(value);
		}
	}

	public bool GetUiGroup(string groupName, out UiGroup uiGroup)
	{
		return groups.TryGetValue(groupName, out uiGroup);
	}

	private void InitMain()
	{
		inventoryQuick = GetEntity<QuickInventoryPanel>(this);
		inventoryMouse = GetEntity<ExchangeInventory>(this);
	}

	private void InitOthers()
	{
		basicTip = GetEntity<BasicTipGroup>(this);
		gameTimeTip = GetEntity<GameTimeTip>(this);
		messageBoxManager = GetEntity<MessageBoxManager>(this);
		sceneOperationTipManager = GetEntity<OperationTipInSceneManager>(this);
		sceneOperationTipMultiManager = GetEntity<OperationTipMultiManager>(this);
		dialogueHistoryTip = GetEntity<DialogueHistoryTip>(this);
		sceneBoxGroup = GetEntity<SceneBoxGroup>(this);
	}

	public T GetFromPool<T>(bool inScene) where T : DolocUiRecyclableObject
	{
		if (!inScene)
		{
			return GetFromPoolInCanvas<T>();
		}
		return GetFromPoolInScene<T>();
	}

	public void RecycleToPool<T>(bool inScene, T value) where T : DolocUiRecyclableObject
	{
		if (inScene)
		{
			RecycleToPoolInScene(value);
		}
		else
		{
			RecycleToPoolInCanvas(value);
		}
	}

	public T GetFromPoolInScene<T>() where T : DolocUiRecyclableObject
	{
		return sceneUiPoolManager.GetFromPool<T>();
	}

	public void RecycleToPoolInScene<T>(T value) where T : DolocUiRecyclableObject
	{
		sceneUiPoolManager.RecycleToPool(value);
	}

	public T GetFromPoolInCanvas<T>() where T : DolocUiRecyclableObject
	{
		return canvasUiPoolManager.GetFromPool<T>();
	}

	public void RecycleToPoolInCanvas<T>(T value) where T : DolocUiRecyclableObject
	{
		canvasUiPoolManager.RecycleToPool(value);
	}

	public Transform GetContainerFromPool<T>(bool inScene) where T : DolocUiRecyclableObject
	{
		if (!inScene)
		{
			return canvasUiPoolManager.GetContainer<T>();
		}
		return sceneUiPoolManager.GetContainer<T>();
	}

	public void ClearPool()
	{
		canvasUiPoolManager.Clear();
		sceneUiPoolManager.Clear();
	}
}
