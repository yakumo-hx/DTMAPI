using DolocTown.Config;
using Yarn.Markup;
using Yarn.Unity;

namespace DolocTown;

public class LocalizedLine : Yarn.Unity.LocalizedLine
{
	public new string CharacterName
	{
		get
		{
			if (base.CharacterName == null || !DolocConfig.Tables.TbDialogueEntity.DataMap.ContainsKey(base.CharacterName))
			{
				return null;
			}
			return base.CharacterName;
		}
	}

	public new MarkupParseResult TextWithoutCharacterName
	{
		get
		{
			if (CharacterName != null)
			{
				return base.TextWithoutCharacterName;
			}
			return base.Text;
		}
	}
}
