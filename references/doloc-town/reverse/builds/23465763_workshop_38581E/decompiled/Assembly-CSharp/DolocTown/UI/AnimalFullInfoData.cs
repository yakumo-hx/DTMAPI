using DolocTown.Config;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public class AnimalFullInfoData : AnimalBaseInfoData
{
	public string birthdayInfo;

	public string growthInfo;

	public string stateInfo;

	public string stateDescription;

	public string positionInfo;

	public string energyInfo;

	public string moodInfo;

	public string spaceInfo;

	public Sprite icon;

	public string callButtonText;

	public bool visible;

	public AnimalFullInfoData(Animal animal)
		: base(animal)
	{
		energyInfo = Mathf.Clamp(Mathf.RoundToInt(energyProgress * 100f), 0, 100) + "/100";
		moodInfo = Mathf.Clamp(Mathf.RoundToInt(moodProgress * 100f), 0, 100) + "/100";
		icon = animal.Icon;
		birthdayInfo = animal.BirthdayInfo;
		spaceInfo = DolocConfig.StaticTexts.UiAnimalSpace.Format(animal.proto.Space);
		growthInfo = (animal.IsAdult ? DolocConfig.StaticTexts.UiAnimalAdult : DolocConfig.StaticTexts.UiAnimalChild);
		stateDescription = DolocConfig.Tables.TbAnimalState.GetOrDefault(animal.CurrentState)?.Description ?? "";
		stateInfo = DolocUtils.Format(DolocConfig.StaticTexts.UiAnimalInfoState, stateBaseInfo);
		positionInfo = DolocUtils.Format(DolocConfig.StaticTexts.UiAnimalPosition, animal.currentRoom.SceneConfig.Title);
		callButtonText = ((!DolocAPI.archiveHandle.IsResonatorUnlocked()) ? "" : (animal.currentRoom.IsInHouse ? DolocConfig.StaticTexts.UiAnimalLetOut : DolocConfig.StaticTexts.UiAnimalCallBack));
		visible = animal.IsInfoVisible;
	}
}
