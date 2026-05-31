using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class UIEntityGroupInfo : BeanBase
{
	public const int __ID__ = -1986804452;

	public string Id { get; private set; }

	public int Order { get; private set; }

	public bool InScene { get; private set; }

	public bool ShowInPhotoState { get; private set; }

	public UIEntityGroupInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
		if (!_json["in_scene"].IsBoolean)
		{
			throw new SerializationException();
		}
		InScene = _json["in_scene"];
		if (!_json["show_in_photo_state"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShowInPhotoState = _json["show_in_photo_state"];
	}

	public UIEntityGroupInfo(string id, int order, bool in_scene, bool show_in_photo_state)
	{
		Id = id;
		Order = order;
		InScene = in_scene;
		ShowInPhotoState = show_in_photo_state;
	}

	public static UIEntityGroupInfo DeserializeUIEntityGroupInfo(JSONNode _json)
	{
		return new UIEntityGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1986804452;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Order:" + Order + ",InScene:" + InScene + ",ShowInPhotoState:" + ShowInPhotoState + ",}";
	}
}
