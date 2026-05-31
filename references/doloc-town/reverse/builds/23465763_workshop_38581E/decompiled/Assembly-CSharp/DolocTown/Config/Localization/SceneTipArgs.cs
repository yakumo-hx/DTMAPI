using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class SceneTipArgs : TipTextArgsBase
{
	public const int __ID__ = 675980065;

	public SpriteAsset Icon { get; private set; }

	public bool ShouldInteract { get; private set; }

	public SceneTipArgs(JSONNode _json)
		: base(_json)
	{
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
		if (!_json["should_interact"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShouldInteract = _json["should_interact"];
	}

	public SceneTipArgs(AlignmentText content, SpriteAsset icon, bool should_interact)
		: base(content)
	{
		Icon = icon;
		ShouldInteract = should_interact;
	}

	public static SceneTipArgs DeserializeSceneTipArgs(JSONNode _json)
	{
		return new SceneTipArgs(_json);
	}

	public override int GetTypeId()
	{
		return 675980065;
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
		return "{ Content:" + base.Content?.ToString() + ",Icon:" + Icon?.ToString() + ",ShouldInteract:" + ShouldInteract + ",}";
	}
}
