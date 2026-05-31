using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.UI;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public abstract class TipTextArgsBase : BeanBase
{
	public AlignmentText Content { get; private set; }

	public TipTextArgsBase(JSONNode _json)
	{
		if (!_json["content"].IsObject)
		{
			throw new SerializationException();
		}
		Content = AlignmentText.DeserializeAlignmentText(_json["content"]);
	}

	public TipTextArgsBase(AlignmentText content)
	{
		Content = content;
	}

	public static TipTextArgsBase DeserializeTipTextArgsBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"UITextConfirmBoxArgs" => new UITextConfirmBoxArgs(_json), 
			"SceneTextBoxArgs" => new SceneTextBoxArgs(_json), 
			"UISmallMessageBoxArgs" => new UISmallMessageBoxArgs(_json), 
			"UINodeMessageBoxArgs" => new UINodeMessageBoxArgs(_json), 
			"UIBigMessageBoxArgs" => new UIBigMessageBoxArgs(_json), 
			"SceneTipArgs" => new SceneTipArgs(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
		Content?.Resolve(_tables);
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
		Content?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Content:" + Content?.ToString() + ",}";
	}
}
