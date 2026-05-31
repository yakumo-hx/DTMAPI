using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct TechPointSimpleData : IUIData
{
	public bool notEmpty { get; }

	public Sprite icon { get; }

	public string count { get; }

	public float progress { get; }

	public TechPointSimpleData(TechLevelData data)
	{
		this = default(TechPointSimpleData);
		if (data != null)
		{
			notEmpty = true;
			icon = data.Icon;
			count = DolocAPI.archiveHandle.GetTechPoint(data.type).ToString();
			progress = DolocAPI.archiveHandle.GetTechPointExpProgress(data.type);
		}
	}
}
