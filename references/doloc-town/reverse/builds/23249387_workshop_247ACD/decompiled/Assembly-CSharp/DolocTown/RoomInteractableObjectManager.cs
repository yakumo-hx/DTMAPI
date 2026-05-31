using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class RoomInteractableObjectManager
{
	[JsonProperty]
	private HashSet<string> objectsInvisible;

	[JsonProperty]
	private HashSet<string> objectsToggledOn;

	[JsonProperty]
	private Dictionary<string, LinearInventory> inventories;

	private GlobalInteractableObjectManager globalManager => DolocAPI.archiveHandle.cityData.globalInteractableObjectManager;

	[JsonConstructor]
	public RoomInteractableObjectManager(HashSet<string> objectsInvisible = null, HashSet<string> objectsToggledOn = null, Dictionary<string, LinearInventory> inventories = null, Dictionary<string, int> interactTimes = null)
	{
		this.objectsInvisible = objectsInvisible ?? new HashSet<string>();
		this.objectsToggledOn = objectsToggledOn ?? new HashSet<string>();
		this.inventories = inventories ?? new Dictionary<string, LinearInventory>();
		foreach (string item in this.inventories.Keys.Where((string key) => this.inventories[key] == null))
		{
			this.inventories.Remove(item);
		}
	}

	public bool LoadObjectVisibleState(string guid, out bool visible)
	{
		visible = false;
		if (string.IsNullOrEmpty(guid))
		{
			return false;
		}
		visible = !objectsInvisible.TryGetValue(guid, out var _);
		return true;
	}

	public void SaveObjectVisibleState(string guid, bool visible)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			if (visible)
			{
				objectsInvisible.Remove(guid);
			}
			else
			{
				objectsInvisible.Add(guid);
			}
		}
	}

	public bool LoadLockState(string lockId)
	{
		return globalManager.LoadLockState(lockId);
	}

	public void SaveLockState(string lockId, bool value)
	{
		globalManager.SaveLockState(lockId, value);
	}

	public bool LoadToggleState(string guid)
	{
		if (!guid.IsNullOrEmpty())
		{
			return objectsToggledOn.Contains(guid);
		}
		return false;
	}

	public void SaveToggleState(string guid, bool value)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			if (!value)
			{
				objectsToggledOn.Remove(guid);
			}
			else
			{
				objectsToggledOn.Add(guid);
			}
		}
	}

	public bool LoadInventory(string guid, out LinearInventory inventory)
	{
		inventory = null;
		if (string.IsNullOrEmpty(guid))
		{
			return false;
		}
		return inventories.TryGetValue(guid, out inventory);
	}

	public void SaveInventory(string guid, LinearInventory value)
	{
		if (!string.IsNullOrEmpty(guid))
		{
			inventories[guid] = value;
		}
	}

	public bool TryGetLogicEntity<T>(InteractableObject obj, out T logic) where T : InteractableObjectLogic
	{
		string roomId = obj.roomId;
		logic = globalManager.GetLogicEntity(roomId, obj.guid) as T;
		logic?.SetPosition(obj.position2d);
		return logic != null;
	}

	public void RegisterLogicEntity(InteractableObject obj, InteractableObjectLogic logic)
	{
		logic?.SetPosition(obj.position2d);
		globalManager.RegisterLogicEntity(logic);
	}

	public void UnregisterLogicEntity(InteractableObject obj, InteractableObjectLogic logic)
	{
		globalManager.UnregisterLogicEntity(logic);
	}
}
