using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown.UI;

public struct MapTipData : IUIData
{
	public string missionId;

	public string missionTitle;

	public string missionTip;

	public Sprite[] missionTypeIcons;

	public Sprite[] missionTypeBigIcons;

	public Sprite[] positionTypeIcons;

	public string[] roomIds;

	public string[] positionInfos;

	public Vector2[] worldPositions;

	public MapMissionTipType[] tipTypes;

	public int mapTipCount;

	public bool notEmpty { get; }

	public MapTipData(IMission mission)
	{
		this = default(MapTipData);
		DolocTown.Config.Mission.MapMissionTip[] array = mission?.NodeInfo?.MapTips?.Where((DolocTown.Config.Mission.MapMissionTip x) => x.TipType != MapMissionTipType.None).ToArray();
		if (array == null || array.Length == 0)
		{
			return;
		}
		missionId = mission.Id;
		mapTipCount = array.Length;
		notEmpty = true;
		missionTitle = mission.Title;
		missionTip = mission.Tip.ClearRichTextLabel();
		roomIds = new string[mapTipCount];
		positionInfos = new string[mapTipCount];
		worldPositions = new Vector2[mapTipCount];
		missionTypeIcons = new Sprite[mapTipCount];
		missionTypeBigIcons = new Sprite[mapTipCount];
		positionTypeIcons = new Sprite[mapTipCount];
		tipTypes = new MapMissionTipType[mapTipCount];
		for (int i = 0; i < mapTipCount; i++)
		{
			DolocTown.Config.Mission.MapMissionTip mapMissionTip = array[i];
			string tipArg = mapMissionTip.TipArg;
			tipTypes[i] = mapMissionTip.TipType;
			switch (mapMissionTip.TipType)
			{
			case MapMissionTipType.MarkPoint:
			{
				MarkPointInfo orDefault = DolocConfig.Tables.TbMarkPoint.GetOrDefault(tipArg);
				if (orDefault == null)
				{
					continue;
				}
				roomIds[i] = orDefault.RoomId;
				worldPositions[i] = orDefault.Position;
				break;
			}
			case MapMissionTipType.Room:
				roomIds[i] = tipArg;
				break;
			case MapMissionTipType.NPC:
			{
				if (!DolocAPI.QueryNpc(tipArg, out var npc))
				{
					continue;
				}
				if (npc.TryGetCurrentRoom(out var room))
				{
					roomIds[i] = room.RoomId;
					worldPositions[i] = npc.positionWS;
				}
				break;
			}
			}
			ref string reference = ref roomIds[i];
			if (reference == null)
			{
				reference = "";
			}
			string text = (DolocConfig.Tables.TbScene.GetOrDefault(roomIds[i]) ?? DolocConfig.Tables.TbScene.GetOrDefault(roomIds[i].Split(".")[0]))?.Title ?? "???";
			string[] array2 = positionInfos;
			int num = i;
			string text2 = ((mapMissionTip.TipType != MapMissionTipType.NPC) ? text : DolocUtils.Format(DolocConfig.StaticTexts.UiMissionNpcPosition, DolocAPI.GetNpcTitle(tipArg, ignoreUnknown: true), text));
			array2[num] = text2;
			missionTypeIcons[i] = mission.BaseInfo.MissionType_Ref?.MapIcon.Asset;
			missionTypeBigIcons[i] = mission.BaseInfo.MissionType_Ref?.MapIconBig.Asset;
			positionTypeIcons[i] = mapMissionTip.TipType_Ref?.Icon.Asset;
		}
	}
}
