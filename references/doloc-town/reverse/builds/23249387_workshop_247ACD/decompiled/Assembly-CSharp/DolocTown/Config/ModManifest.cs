using System.Collections.Generic;
using Newtonsoft.Json;

namespace DolocTown.Config;

[JsonObject(MemberSerialization.OptIn)]
public class ModManifest
{
	[JsonProperty]
	public string name;

	[JsonProperty]
	public string author;

	[JsonProperty]
	public string version;

	[JsonProperty]
	public string description;

	[JsonProperty]
	public string[] tags;

	[JsonProperty("localized_name")]
	public Dictionary<string, string> nameL10n;

	[JsonProperty("localized_description")]
	public Dictionary<string, string> descriptionL10n;

	public string Title => GetL10nText(DolocAPI.CurrentL10nInfo.SteamL10nId, name, nameL10n);

	public string Description => GetL10nText(DolocAPI.CurrentL10nInfo.SteamL10nId, description, descriptionL10n);

	public static string[] GetUploadL10nIds()
	{
		return new string[3] { "schinese", "tchinese", "english" };
	}

	public string GetL10nIdTitle(string l10nId)
	{
		return GetL10nText(l10nId, name, nameL10n);
	}

	public string GetL10nIdDescription(string l10nId)
	{
		return GetL10nText(l10nId, description, descriptionL10n);
	}

	private string GetL10nText(string l10nId, string fallback, Dictionary<string, string> localizedTexts)
	{
		if (localizedTexts.IsNullOrEmpty() || l10nId.IsNullOrEmpty())
		{
			return fallback;
		}
		if (localizedTexts.TryGetValue(l10nId, out var value) && !value.IsNullOrEmpty())
		{
			return value;
		}
		return fallback;
	}
}
