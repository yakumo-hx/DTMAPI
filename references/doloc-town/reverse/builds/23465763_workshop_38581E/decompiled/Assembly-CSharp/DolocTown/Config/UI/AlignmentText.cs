using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class AlignmentText : BeanBase
{
	public const int __ID__ = -708373578;

	public string Text { get; private set; }

	public string Text_l10n_key { get; }

	public TextAnchor Alignment { get; private set; }

	public AlignmentText(JSONNode _json)
	{
		if (!_json["text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Text_l10n_key = _json["text"]["key"];
		if (!_json["text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Text = _json["text"]["text"];
		if (!_json["alignment"].IsNumber)
		{
			throw new SerializationException();
		}
		Alignment = (TextAnchor)_json["alignment"].AsInt;
	}

	public AlignmentText(string text, TextAnchor alignment)
	{
		Text = text;
		Alignment = alignment;
	}

	public static AlignmentText DeserializeAlignmentText(JSONNode _json)
	{
		return new AlignmentText(_json);
	}

	public override int GetTypeId()
	{
		return -708373578;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Text = translator(Text_l10n_key, Text);
	}

	public override string ToString()
	{
		return "{ Text:" + Text + ",Alignment:" + Alignment.ToString() + ",}";
	}
}
