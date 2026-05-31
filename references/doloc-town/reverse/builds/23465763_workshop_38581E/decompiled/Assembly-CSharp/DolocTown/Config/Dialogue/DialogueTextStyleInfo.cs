using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Dialogue;

public sealed class DialogueTextStyleInfo : BeanBase
{
	public readonly Dictionary<bool, TextLabel> Labels_Index = new Dictionary<bool, TextLabel>();

	public const int __ID__ = -237636412;

	public string Id { get; private set; }

	public TextLabel[] Labels { get; private set; }

	public DialogueTextStyleInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["labels"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Labels = new TextLabel[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TextLabel textLabel = TextLabel.DeserializeTextLabel(child);
			Labels[num++] = textLabel;
		}
		TextLabel[] labels = Labels;
		foreach (TextLabel textLabel2 in labels)
		{
			Labels_Index.Add(textLabel2.UseBounceEffect, textLabel2);
		}
	}

	public DialogueTextStyleInfo(string id, TextLabel[] labels)
	{
		Id = id;
		Labels = labels;
		TextLabel[] labels2 = Labels;
		foreach (TextLabel textLabel in labels2)
		{
			Labels_Index.Add(textLabel.UseBounceEffect, textLabel);
		}
	}

	public static DialogueTextStyleInfo DeserializeDialogueTextStyleInfo(JSONNode _json)
	{
		return new DialogueTextStyleInfo(_json);
	}

	public override int GetTypeId()
	{
		return -237636412;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TextLabel[] labels = Labels;
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TextLabel[] labels = Labels;
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Labels:" + StringUtil.CollectionToString(Labels) + ",}";
	}
}
