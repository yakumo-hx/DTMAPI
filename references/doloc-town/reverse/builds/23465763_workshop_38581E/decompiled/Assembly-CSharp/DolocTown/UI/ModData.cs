using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public class ModData : IUIData
{
	public string name;

	public string author;

	public string description;

	public string version;

	public Sprite icon;

	public ModSourceType sourceType;

	public bool enabled;

	public bool canUpdateWorkshopItem;

	public bool canMoveUp;

	public bool canMoveDown;

	public string[] tags;

	public bool notEmpty { get; }

	public ModData(ModInfo info, WorkshopUploadPlan uploadPlan = null)
	{
		if (info?.manifest != null)
		{
			notEmpty = true;
			name = info.title;
			version = info.manifest.version;
			author = info.manifest.author;
			description = info.description;
			tags = info.manifest.tags;
			icon = info.icon;
			sourceType = info.source;
			enabled = info.enabled;
			canUpdateWorkshopItem = info.source == ModSourceType.Local && uploadPlan != null && uploadPlan.mode == WorkshopUploadMode.Update;
			canMoveUp = enabled && DolocAPI.modManager.CanMovePrev(info);
			canMoveDown = enabled && info.priority > 0 && DolocAPI.modManager.CanMoveNext(info);
		}
	}
}
