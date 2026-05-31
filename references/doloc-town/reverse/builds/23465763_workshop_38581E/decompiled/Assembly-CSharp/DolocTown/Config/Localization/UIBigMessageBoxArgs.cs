using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class UIBigMessageBoxArgs : TipTextArgsBase
{
	public const int __ID__ = 1109948792;

	public SpriteAsset Icon { get; private set; }

	public UIBigMessageBoxArgs(JSONNode _json)
		: base(_json)
	{
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
	}

	public UIBigMessageBoxArgs(AlignmentText content, SpriteAsset icon)
		: base(content)
	{
		Icon = icon;
	}

	public static UIBigMessageBoxArgs DeserializeUIBigMessageBoxArgs(JSONNode _json)
	{
		return new UIBigMessageBoxArgs(_json);
	}

	public override int GetTypeId()
	{
		return 1109948792;
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
		return "{ Content:" + base.Content?.ToString() + ",Icon:" + Icon?.ToString() + ",}";
	}
}
