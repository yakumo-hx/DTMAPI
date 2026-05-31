using System.Collections.Generic;
using System.Globalization;
using DolocTown.Config;
using DolocTown.Config.Localization;
using UnityEngine;
using Yarn;
using Yarn.Unity;

namespace DolocTown;

public class TextLineProvider
{
	private readonly YarnProject yarnProject;

	public string textLanguageCode = CultureInfo.CurrentCulture.Name;

	public bool LinesAvailable => true;

	public string LocaleCode => textLanguageCode;

	public TextLineProvider(YarnProject project)
	{
		yarnProject = project;
	}

	public LocalizedLine GetLocalizedLine(Line line)
	{
		string rawText = yarnProject.GetLocalization(textLanguageCode).GetLocalizedString(line.ID) ?? ("missing:<" + line.ID + ">");
		return new LocalizedLine
		{
			TextID = line.ID,
			RawText = rawText,
			Substitutions = line.Substitutions,
			Metadata = yarnProject.lineMetadata.GetMetadata(line.ID)
		};
	}

	public void PrepareForLines(IEnumerable<string> lineIDs)
	{
	}

	public void SwitchLanguage(string l10nId)
	{
		LocalizationInfo orDefault = DolocConfig.Tables.TbLocalization.GetOrDefault(l10nId);
		if (orDefault == null)
		{
			Debug.LogError("未知的l10nId: " + l10nId);
		}
		else
		{
			textLanguageCode = orDefault.DialogueL10nId;
		}
	}
}
