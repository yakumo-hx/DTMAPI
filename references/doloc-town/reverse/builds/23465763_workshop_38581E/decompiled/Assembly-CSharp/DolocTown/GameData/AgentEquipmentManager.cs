using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Player;
using DolocTown.Params;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class AgentEquipmentManager
{
	private readonly Dictionary<Item, AgentEquipmentFunction> functions = new Dictionary<Item, AgentEquipmentFunction>();

	[JsonProperty]
	public Item hatItem { get; private set; }

	[JsonProperty]
	public Item droneItem { get; private set; }

	[JsonProperty]
	public Item activeItem { get; private set; }

	[JsonProperty]
	public Item passiveItem1 { get; private set; }

	[JsonProperty]
	public Item passiveItem2 { get; private set; }

	public AgentEquipmentAbility EquipmentAbility { get; private set; }

	public bool HasHat => hatItem != null;

	public bool HasDrone => droneItem != null;

	public bool HasActiveItem => activeItem != null;

	public bool HasPassiveItem1 => passiveItem1 != null;

	public bool HasPassiveItem2 => passiveItem2 != null;

	public bool IsChomperMimicryForbidden
	{
		get
		{
			foreach (AgentEquipmentFunction value in functions.Values)
			{
				if (value.IsChomperMimicryForbidden)
				{
					return true;
				}
			}
			return false;
		}
	}

	public AgentEquipmentManager()
	{
		hatItem = null;
		droneItem = null;
		activeItem = null;
		passiveItem1 = null;
		passiveItem2 = null;
	}

	[JsonConstructor]
	public AgentEquipmentManager(Item hatItem = null, Item droneItem = null, Item activeItem = null, Item passiveItem1 = null, Item passiveItem2 = null)
	{
		this.hatItem = hatItem?.CheckValid();
		this.droneItem = droneItem?.CheckValid();
		this.activeItem = activeItem?.CheckValid();
		this.passiveItem1 = passiveItem1?.CheckValid();
		this.passiveItem2 = passiveItem2?.CheckValid();
	}

	public void OverwriteAll(AgentEquipmentManager other)
	{
		DolocAPI.EquipDrone(other.droneItem, out var oldDrone);
		DolocAPI.EquipHat(other.hatItem?.name ?? "", out oldDrone);
		DolocAPI.EquipActiveItem(other.activeItem, out oldDrone);
		DolocAPI.EquipPassiveItem1(other.passiveItem1, out oldDrone);
		DolocAPI.EquipPassiveItem2(other.passiveItem2, out oldDrone);
	}

	public void AfterLoadData()
	{
		HatInfo hatInfo = ((hatItem != null) ? DolocConfig.Tables.TbHat.GetOrDefault(hatItem.name) : null);
		DolocAPI.agent.SetHatInfo(hatInfo);
		DolocAPI.RunDrone((droneItem as ItemDroneStructure)?.droneStructure);
		AddAgentEquipmentFunction(hatItem);
		AddAgentEquipmentFunction(passiveItem1);
		AddAgentEquipmentFunction(passiveItem2);
		ReloadParams();
	}

	public void DEBUG_AddFunction(string funcName)
	{
		if (functions.Values.Any((AgentEquipmentFunction existingFunc) => existingFunc.SkillId == funcName))
		{
			Debug.LogWarning("技能对象\"" + funcName + "\"已存在，无法重复添加");
			return;
		}
		Item item = DolocAPI.GenerateItem("seed_endyam");
		if (!AgentEquipmentFunction.CreateAgentEquipmentFunction(item, this, funcName, out var function))
		{
			Debug.LogError("创建技能对象\"" + funcName + "\"失败");
			return;
		}
		functions.Add(item, function);
		ReloadParams();
	}

	public void DEBUG_RemoveFunction(string funcName)
	{
		KeyValuePair<Item, AgentEquipmentFunction> keyValuePair = functions.FirstOrDefault((KeyValuePair<Item, AgentEquipmentFunction> kv) => kv.Value.SkillId == funcName);
		if (keyValuePair.Value == null)
		{
			Debug.LogWarning("技能对象\"" + funcName + "\"不存在，无法移除");
			return;
		}
		functions.Remove(keyValuePair.Key);
		keyValuePair.Value.Dispose();
		ReloadParams();
	}

	public void DEBUG_ClearDebugFunction()
	{
		foreach (KeyValuePair<Item, AgentEquipmentFunction> item in functions.Where((KeyValuePair<Item, AgentEquipmentFunction> x) => x.Key.name == "seed_endyam").ToList())
		{
			functions.Remove(item.Key);
			item.Value.Dispose();
			Debug.Log("移除技能对象\"" + item.Value.SkillId + "\"");
		}
	}

	public void ReloadParams()
	{
		int valueOrDefault = (((ItemFunctionHatBase)(hatItem?.proto?.Function))?.HatId_Ref?.Defense).GetValueOrDefault();
		AgentEquipmentParams agentEquipmentParams = new AgentEquipmentParams(valueOrDefault);
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			agentEquipmentParams = value.DoExtraConfig(agentEquipmentParams);
		}
		MotionParamSO motionParam = DolocAPI.assets.motionParam;
		EquipmentAbility = agentEquipmentParams.Commit(motionParam);
		DolocAPI.uiSystem.agentStatusBar.UpdateHealth();
	}

	private string GetSkillFromItem(Item item)
	{
		if (item?.proto.Function is ItemFunctionHatBase itemFunctionHatBase && !itemFunctionHatBase.HatId_Ref.Skill.IsNullOrEmpty())
		{
			return itemFunctionHatBase.HatId_Ref.Skill;
		}
		if (item?.proto.Function is ItemFunctionPassive itemFunctionPassive && !itemFunctionPassive.Skill.IsNullOrEmpty())
		{
			return itemFunctionPassive.Skill;
		}
		if (item?.proto.Function is ItemFunctionHerbPackage itemFunctionHerbPackage && !itemFunctionHerbPackage.Skill.IsNullOrEmpty())
		{
			return itemFunctionHerbPackage.Skill;
		}
		return null;
	}

	private AgentEquipmentFunction CreateAgentEquipmentFunction(Item item)
	{
		string skillFromItem = GetSkillFromItem(item);
		if (skillFromItem.IsNullOrEmpty())
		{
			Debug.LogWarning("该道具没有关联任何主角装备技能：" + item?.name);
			return null;
		}
		if (!AgentEquipmentFunction.CreateAgentEquipmentFunction(item, this, skillFromItem, out var function))
		{
			return null;
		}
		return function;
	}

	private bool AddAgentEquipmentFunction(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (functions.ContainsKey(item))
		{
			return false;
		}
		AgentEquipmentFunction agentEquipmentFunction = CreateAgentEquipmentFunction(item);
		if (agentEquipmentFunction == null)
		{
			return false;
		}
		functions.Add(item, agentEquipmentFunction);
		Debug.Log("<color=cyan>添加主角装备技能：</color>" + agentEquipmentFunction.SkillId);
		return true;
	}

	private void RemoveAgentEquipmentFunction(Item item)
	{
		if (item != null && functions.Remove(item, out var value))
		{
			value.Dispose();
		}
	}

	public void SendMessage(GameMessage message)
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			value.OnReceiveMessage(message);
		}
	}

	public void UpdatePerTu()
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			value.UpdatePerTu();
		}
	}

	public void UpdatePerTuNoRender()
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			value.UpdatePerTuNoRender();
		}
	}

	public void AfterEnterRoom(Room room)
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			value.AfterEnterRoom(room);
		}
	}

	public bool TryResistFaint(bool shouldRender = false)
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			if (value.TryResistFaint(shouldRender))
			{
				return true;
			}
		}
		return false;
	}

	public bool EquipHat(Item hatItem, out Item oldHat)
	{
		if (hatItem == null)
		{
			oldHat = this.hatItem;
			RemoveAgentEquipmentFunction(oldHat);
			this.hatItem = null;
			ReloadParams();
			return true;
		}
		if (!(hatItem is ItemHat))
		{
			oldHat = null;
			return false;
		}
		oldHat = this.hatItem;
		RemoveAgentEquipmentFunction(oldHat);
		this.hatItem = hatItem;
		AddAgentEquipmentFunction(hatItem);
		ReloadParams();
		return true;
	}

	public bool EquipDrone(Item newDroneItem, out Item oldDrone)
	{
		if (newDroneItem == null)
		{
			oldDrone = droneItem;
			droneItem = null;
			return true;
		}
		if (!(newDroneItem is ItemDroneStructure))
		{
			oldDrone = null;
			return false;
		}
		oldDrone = droneItem;
		droneItem = newDroneItem;
		return true;
	}

	public bool IsEquippedDrone(Item item)
	{
		return item == droneItem;
	}

	public bool EquipActiveItem(Item item, out Item oldItem)
	{
		if (item == null)
		{
			oldItem = activeItem;
			activeItem = null;
			return true;
		}
		if (!(item is IActiveItem))
		{
			oldItem = null;
			return false;
		}
		oldItem = activeItem;
		activeItem = item;
		return true;
	}

	public void UseActiveAsItem()
	{
		activeItem?.UseAsItem();
	}

	public void UseActiveAsTool()
	{
		activeItem?.UseAsTool();
	}

	public bool IsEquippedActive(Item item)
	{
		return item == activeItem;
	}

	public bool EquipPassiveItem1(Item item, out Item oldItem)
	{
		if (item == null)
		{
			oldItem = passiveItem1;
			RemoveAgentEquipmentFunction(oldItem);
			passiveItem1 = null;
			ReloadParams();
			return true;
		}
		if (!(item is ItemPassive))
		{
			oldItem = null;
			return false;
		}
		oldItem = passiveItem1;
		RemoveAgentEquipmentFunction(oldItem);
		passiveItem1 = item;
		AddAgentEquipmentFunction(passiveItem1);
		ReloadParams();
		return true;
	}

	public bool EquipPassiveItem2(Item item, out Item oldItem)
	{
		if (item == null)
		{
			oldItem = passiveItem2;
			RemoveAgentEquipmentFunction(oldItem);
			passiveItem2 = null;
			ReloadParams();
			return true;
		}
		if (!(item is ItemPassive))
		{
			oldItem = null;
			return false;
		}
		oldItem = passiveItem2;
		RemoveAgentEquipmentFunction(oldItem);
		passiveItem2 = item;
		AddAgentEquipmentFunction(passiveItem2);
		ReloadParams();
		return true;
	}

	public bool IsEquippedPassive1(Item item)
	{
		return item == passiveItem1;
	}

	public bool IsEquippedPassive2(Item item)
	{
		return item == passiveItem2;
	}

	public bool TryGetAgentEquipmentFunction<T>(out T function) where T : AgentEquipmentFunction
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			if (value is T val)
			{
				function = val;
				return true;
			}
		}
		function = null;
		return false;
	}

	public void SetFishingPoolName(string poolName)
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			value.SetFishingPoolName(poolName);
		}
	}

	public bool IsShepherdActive(string name, out int moodIncrease)
	{
		moodIncrease = 0;
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			if (value.IsShepherdActive(name, out moodIncrease))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryGetShieldItem(out IAgentEquipmentShieldItem item)
	{
		foreach (AgentEquipmentFunction value in functions.Values)
		{
			if (value is IAgentEquipmentShieldItem agentEquipmentShieldItem)
			{
				item = agentEquipmentShieldItem;
				return true;
			}
		}
		item = null;
		return false;
	}
}
