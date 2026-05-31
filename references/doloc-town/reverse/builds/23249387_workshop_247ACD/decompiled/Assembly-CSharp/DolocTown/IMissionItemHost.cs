using DolocTown.Config.Item;
using DolocTown.Config.Room;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public interface IMissionItemHost
{
	[JsonProperty]
	MissionItemManager DM_missionItem { get; }

	void RenderAllMissionItems()
	{
		DolocAPI.EntitySystem.SetupAll<MissionItemRenderer, MissionItem>(DM_missionItem.allItems, RenderMissionItem);
	}

	void HideAllMissionItems()
	{
		foreach (MissionItem allItem in DM_missionItem.allItems)
		{
			DolocAPI.EntitySystem.Recycle(allItem.Renderer);
		}
	}

	void ClearAndRenderAllMissionItems()
	{
		foreach (MissionItem allItem in DM_missionItem.allItems)
		{
			DolocAPI.EntitySystem.Recycle(allItem.Renderer);
		}
		RenderAllMissionItems();
	}

	bool CreateMissionItemNoRender(MissionItemInfo itemProto, MarkPointInfo markPointProto)
	{
		if (itemProto == null || markPointProto == null)
		{
			return false;
		}
		DM_missionItem.CreateMissionItem(itemProto, markPointProto.Position).host = this;
		return true;
	}

	bool RemoveMissionItem(MissionItem item)
	{
		if (item == null)
		{
			return false;
		}
		if (DM_missionItem.RemoveMissionItem(item))
		{
			DolocAPI.EntitySystem.Recycle(item.Renderer);
			return true;
		}
		return false;
	}

	void RenderMissionItem(MissionItemRenderer renderer, MissionItem missionItem)
	{
		renderer.WorldContent = missionItem;
		missionItem.BaseRenderer = renderer;
		renderer.SetSprite(missionItem.SceneSprite);
		Vector3 position = missionItem.PositionWS;
		position.z = DolocAPI.eftConfig.dropItemZRange;
		renderer.position = position;
		renderer.ToggleItemShine();
	}

	void __AfterLoadMissionItems()
	{
		DolocInitAssert.IsTrue(DM_missionItem.allItems != null);
		foreach (MissionItem allItem in DM_missionItem.allItems)
		{
			allItem.host = this;
		}
	}
}
