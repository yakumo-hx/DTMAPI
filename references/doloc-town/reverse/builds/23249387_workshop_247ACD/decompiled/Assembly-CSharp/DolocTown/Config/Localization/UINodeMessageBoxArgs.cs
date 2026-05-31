using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class UINodeMessageBoxArgs : TipTextArgsBase
{
	public const int __ID__ = 1869246380;

	public SpriteAsset Icon { get; private set; }

	public UINodeMessageBoxArgs(JSONNode _json)
		: base(_json)
	{
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
	}

	public UINodeMessageBoxArgs(AlignmentText content, SpriteAsset icon)
		: base(content)
	{
		Icon = icon;
	}

	public static UINodeMessageBoxArgs DeserializeUINodeMessageBoxArgs(JSONNode _json)
	{
		return new UINodeMessageBoxArgs(_json);
	}

	public override int GetTypeId()
	{
		return 1869246380;
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
