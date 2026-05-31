using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class UITextConfirmBoxArgs : TipTextArgsBase
{
	public const int __ID__ = -803707202;

	public AlignmentText Title { get; private set; }

	public AlignmentText Signature { get; private set; }

	public UITextConfirmBoxArgs(JSONNode _json)
		: base(_json)
	{
		if (!_json["title"].IsObject)
		{
			throw new SerializationException();
		}
		Title = AlignmentText.DeserializeAlignmentText(_json["title"]);
		if (!_json["signature"].IsObject)
		{
			throw new SerializationException();
		}
		Signature = AlignmentText.DeserializeAlignmentText(_json["signature"]);
	}

	public UITextConfirmBoxArgs(AlignmentText content, AlignmentText title, AlignmentText signature)
		: base(content)
	{
		Title = title;
		Signature = signature;
	}

	public static UITextConfirmBoxArgs DeserializeUITextConfirmBoxArgs(JSONNode _json)
	{
		return new UITextConfirmBoxArgs(_json);
	}

	public override int GetTypeId()
	{
		return -803707202;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		Title?.Resolve(_tables);
		Signature?.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
		Title?.TranslateText(translator);
		Signature?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Content:" + base.Content?.ToString() + ",Title:" + Title?.ToString() + ",Signature:" + Signature?.ToString() + ",}";
	}
}
