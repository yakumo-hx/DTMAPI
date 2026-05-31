using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;

namespace DolocTown.UI;

public class ModChangeData : IUIData
{
	public string addListText;

	public string removeListText;

	public bool notEmpty { get; }

	public bool hasAdded => !addListText.IsNullOrEmpty();

	public bool hasRemoved => !removeListText.IsNullOrEmpty();

	public bool hasChanged
	{
		get
		{
			if (!hasAdded)
			{
				return hasRemoved;
			}
			return true;
		}
	}

	public ModChangeData(ModInfo[] oldInfos, ModInfo[] newInfos)
	{
		notEmpty = true;
		Dictionary<string, ModInfo> dictionary = new Dictionary<string, ModInfo>();
		ModInfo[] array = newInfos;
		foreach (ModInfo modInfo in array)
		{
			dictionary[modInfo.id] = modInfo;
		}
		array = oldInfos;
		foreach (ModInfo modInfo2 in array)
		{
			dictionary.Remove(modInfo2.id);
		}
		Dictionary<string, ModInfo> dictionary2 = new Dictionary<string, ModInfo>();
		array = oldInfos;
		foreach (ModInfo modInfo3 in array)
		{
			dictionary2[modInfo3.id] = modInfo3;
		}
		array = newInfos;
		foreach (ModInfo modInfo4 in array)
		{
			dictionary2.Remove(modInfo4.id);
		}
		addListText = string.Join("\n", dictionary.Values.Select((ModInfo x) => " + [" + GetPrefix(x) + "] " + x.savedTitle));
		removeListText = string.Join("\n", dictionary2.Values.Select((ModInfo x) => " - [" + GetPrefix(x) + "] " + x.savedTitle));
	}

	private string GetPrefix(ModInfo info)
	{
		if (info.source == ModSourceType.Local)
		{
			return DolocConfig.StaticTexts.UiModSourceLocal;
		}
		return DolocConfig.StaticTexts.UiModSourceWorkshop;
	}
}
