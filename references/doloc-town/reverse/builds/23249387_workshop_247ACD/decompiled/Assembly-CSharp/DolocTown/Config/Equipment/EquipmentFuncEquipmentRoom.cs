using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncEquipmentRoom : EquipmentFuncEquipment
{
	public const int __ID__ = 1977293783;

	public string RoomName { get; private set; }

	public EquipmentFuncEquipmentRoom(JSONNode _json)
		: base(_json)
	{
		if (!_json["room_name"].IsString)
		{
			throw new SerializationException();
		}
		RoomName = _json["room_name"];
	}

	public EquipmentFuncEquipmentRoom(string room_name)
	{
		RoomName = room_name;
	}

	public static EquipmentFuncEquipmentRoom DeserializeEquipmentFuncEquipmentRoom(JSONNode _json)
	{
		return new EquipmentFuncEquipmentRoom(_json);
	}

	public override int GetTypeId()
	{
		return 1977293783;
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
		return "{ RoomName:" + RoomName + ",}";
	}
}
