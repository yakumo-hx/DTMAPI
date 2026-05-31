using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class MissionItem : WorldContent
{
	private MissionItemInfo proto;

	[JsonProperty]
	public string name => proto.Id;

	public override Sprite SceneSprite => proto.SceneAsset.Asset;

	public IMissionItemHost host { get; set; }

	public MissionItemRenderer Renderer => (MissionItemRenderer)base.BaseRenderer;

	public MissionItem(MissionItemInfo proto, Vector3 pos)
		: base(pos)
	{
		this.proto = proto;
	}

	[JsonConstructor]
	private MissionItem(string name, int index, Vector2 pos)
		: base(index, pos)
	{
		proto = DolocConfig.Tables.TbMissionItem.GetOrDefault(name);
	}

	protected override bool ValidateDeserialization()
	{
		return proto != null;
	}

	public override void OnInteract()
	{
		if (!DolocAPI.QueryItemProto(name, out var itemInfo))
		{
			Debug.LogError("未知的道具Id");
			return;
		}
		string id = itemInfo.Id;
		if (DolocAPI.PlaceItem(id) == null)
		{
			DolocAPI.RaiseItemObtainTip(id, itemInfo.UiSpriteAsset.Asset, itemInfo.Title);
			DolocAPI.RaiseSpriteFadeUp(base.PositionWS, itemInfo.UiSpriteAsset.Asset);
			DolocAPI.BroadcastString(GameEventType.OBTAIN_ITEM, id);
			host.RemoveMissionItem(this);
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrBackpackIsFull);
		}
	}
}
