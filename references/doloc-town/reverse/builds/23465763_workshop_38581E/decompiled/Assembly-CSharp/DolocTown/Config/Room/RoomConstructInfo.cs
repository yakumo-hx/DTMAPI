using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class RoomConstructInfo : BeanBase
{
	public const int __ID__ = 1843089847;

	public bool AllowBuildPlatform { get; private set; }

	public bool AllowBuildResource { get; private set; }

	public bool ConstrainResource { get; private set; }

	public bool AllowBuildBuilding { get; private set; }

	public bool AllowBuildEquipment { get; private set; }

	public bool AllowBuildDecal { get; private set; }

	public RoomConstructInfo(JSONNode _json)
	{
		if (!_json["allowBuildPlatform"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowBuildPlatform = _json["allowBuildPlatform"];
		if (!_json["allowBuildResource"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowBuildResource = _json["allowBuildResource"];
		if (!_json["constrainResource"].IsBoolean)
		{
			throw new SerializationException();
		}
		ConstrainResource = _json["constrainResource"];
		if (!_json["allowBuildBuilding"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowBuildBuilding = _json["allowBuildBuilding"];
		if (!_json["allowBuildEquipment"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowBuildEquipment = _json["allowBuildEquipment"];
		if (!_json["allowBuildDecal"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowBuildDecal = _json["allowBuildDecal"];
	}

	public RoomConstructInfo(bool allowBuildPlatform, bool allowBuildResource, bool constrainResource, bool allowBuildBuilding, bool allowBuildEquipment, bool allowBuildDecal)
	{
		AllowBuildPlatform = allowBuildPlatform;
		AllowBuildResource = allowBuildResource;
		ConstrainResource = constrainResource;
		AllowBuildBuilding = allowBuildBuilding;
		AllowBuildEquipment = allowBuildEquipment;
		AllowBuildDecal = allowBuildDecal;
	}

	public static RoomConstructInfo DeserializeRoomConstructInfo(JSONNode _json)
	{
		return new RoomConstructInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1843089847;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ AllowBuildPlatform:" + AllowBuildPlatform + ",AllowBuildResource:" + AllowBuildResource + ",ConstrainResource:" + ConstrainResource + ",AllowBuildBuilding:" + AllowBuildBuilding + ",AllowBuildEquipment:" + AllowBuildEquipment + ",AllowBuildDecal:" + AllowBuildDecal + ",}";
	}
}
