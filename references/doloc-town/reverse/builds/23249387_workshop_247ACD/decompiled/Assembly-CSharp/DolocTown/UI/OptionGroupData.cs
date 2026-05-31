using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Dialogue;
using Yarn.Markup;

namespace DolocTown.UI;

public struct OptionGroupData : IUIData
{
	public bool notEmpty { get; }

	public OptionData[] options { get; }

	public int count => options.Length;

	public OptionGroupData(DialogueOption[] dialogueOptions)
	{
		this = default(OptionGroupData);
		if (!dialogueOptions.IsNullOrEmpty())
		{
			notEmpty = true;
			int num = dialogueOptions.Length;
			options = new OptionData[num];
			for (int i = 0; i < num; i++)
			{
				DialogueOption dialogueOption = dialogueOptions[i];
				options[i].index = dialogueOption.DialogueOptionID;
				options[i].isVisited = dialogueOption.IsVisited;
				ParseOptionType(dialogueOption.Line.Text, ref options[i]);
			}
			options = SortOptions(options);
		}
	}

	public OptionGroupData(string[] lines)
	{
		this = default(OptionGroupData);
		if (!lines.IsNullOrEmpty())
		{
			notEmpty = true;
			int num = lines.Length;
			options = new OptionData[num];
			for (int i = 0; i < num; i++)
			{
				options[i].index = i;
				options[i].isVisited = false;
				ParseOptionType(DolocAPI.ParseMarkup(lines[i]), ref options[i]);
			}
			options = SortOptions(options);
		}
	}

	private static void ParseOptionType(MarkupParseResult markupLine, ref OptionData optionData)
	{
		if (markupLine.TryGetAttributeWithName(DolocConst.Dialogue_OptionAtrribute, out var attribute))
		{
			if (!attribute.Properties.TryGetValue(DolocConst.Dialogue_OptionTypeProperty, out var value))
			{
				return;
			}
			optionData.type = value.ToString();
			DialogueOptionInfo orDefault = DolocConfig.Tables.TbDialogueOption.GetOrDefault(optionData.type);
			if (orDefault == null)
			{
				SetDefault(ref optionData);
				return;
			}
			optionData.text = orDefault.IconText + DolocAPI.GetTextWithMarkup(markupLine).ClearRichTextLabelExceptColor();
			optionData.typeOrder = orDefault.Order;
			if (attribute.Properties.TryGetValue(DolocConst.Dialogue_OptionOrderProperty, out var value2))
			{
				float.TryParse(value2.ToString(), out optionData.optionOrder);
			}
		}
		else
		{
			SetDefault(ref optionData);
		}
		void SetDefault(ref OptionData optionData)
		{
			optionData.text = DolocConst.Dialogue_IconOptionDefault + DolocAPI.GetTextWithMarkup(markupLine).ClearRichTextLabelExceptColor();
			optionData.typeOrder = 0f;
			optionData.optionOrder = DolocConst.Dialogue_OptionDefaultOrder;
		}
	}

	private OptionData[] SortOptions(OptionData[] options)
	{
		return (from x in options
			orderby x.typeOrder descending, x.optionOrder descending
			select x).ToArray();
	}
}
