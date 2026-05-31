using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class UISmallMessageBoxArgs : TipTextArgsBase
{
	public const int __ID__ = -1652912257;

	public bool ErrorStyle { get; private set; }

	public UISmallMessageBoxArgs(JSONNode _json)
		: base(_json)
	{
		if (!_json["error_style"].IsBoolean)
		{
			throw new SerializationException();
		}
		ErrorStyle = _json["error_style"];
	}

	public UISmallMessageBoxArgs(AlignmentText content, bool error_style)
		: base(content)
	{
		ErrorStyle = error_style;
	}

	public static UISmallMessageBoxArgs DeserializeUISmallMessageBoxArgs(JSONNode _json)
	{
		return new UISmallMessageBoxArgs(_json);
	}

	public override int GetTypeId()
	{
		return -1652912257;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Content:" + base.Content?.ToString() + ",ErrorStyle:" + ErrorStyle + ",}";
	}
}
